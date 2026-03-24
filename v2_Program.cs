using Azure.Storage.Blobs;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         Azure Blob → SharePoint Online File Migrator           ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

        // ============= CONFIGURATION (CHANGE THESE) =============
        const string blobConnectionString = "DefaultEndpointsProtocol=https;AccountName=YOUR_STORAGE_ACCOUNT;AccountKey=YOUR_STORAGE_KEY;EndpointSuffix=core.windows.net";
        const string blobContainerName = "documents";
        const string sharePointSiteUrl = "https://yourtenant.sharepoint.com/sites/yoursite";
        const string sharePointLibraryName = "Documents";
        const string tenantId = "YOUR_TENANT_ID";
        const string clientId = "YOUR_CLIENT_ID";
        const string clientSecret = "YOUR_CLIENT_SECRET";

        try
        {
            // STEP 1: Download 2 files from Azure Blob Storage
            Console.WriteLine("📥 STEP 1: Downloading files from Azure Blob Storage...\n");

            var blobFiles = await GetFilesFromBlob(blobConnectionString, blobContainerName);

            if (blobFiles.Count == 0)
            {
                Console.WriteLine("❌ No files found in blob container!");
                return;
            }

            Console.WriteLine($"✅ Found {blobFiles.Count} files in blob:\n");
            foreach (var file in blobFiles)
            {
                Console.WriteLine($"   - {file.Name} ({file.Size} bytes)");
            }

            // STEP 2: Upload files to SharePoint
            Console.WriteLine("\n📤 STEP 2: Uploading files to SharePoint...\n");

            var graphClient = GetGraphClient(tenantId, clientId, clientSecret);
            var uploadedFiles = await UploadToSharePoint(graphClient, sharePointSiteUrl, sharePointLibraryName, blobFiles);

            // STEP 3: Display Results
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                     MIGRATION COMPLETE                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("✅ Successfully migrated files:\n");
            foreach (var file in uploadedFiles)
            {
                Console.WriteLine($"   📄 {file.Name}");
                Console.WriteLine($"      URL: {file.Url}\n");
            }

            Console.WriteLine("Done! All files have been migrated to SharePoint.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ERROR: {ex.Message}");
            Console.WriteLine($"Details: {ex.InnerException?.Message}");
        }
    }

    // Download files from Azure Blob Storage
    static async Task<List<BlobFile>> GetFilesFromBlob(string connectionString, string containerName)
    {
        var files = new List<BlobFile>();
        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            var download = await blobClient.DownloadAsync();

            var memoryStream = new MemoryStream();
            await download.Value.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            files.Add(new BlobFile
            {
                Name = blobItem.Name,
                Size = blobItem.Properties.ContentLength ?? 0,
                Stream = memoryStream
            });
        }

        return files;
    }

    // Get GraphServiceClient for SharePoint access
    static GraphServiceClient GetGraphClient(string tenantId, string clientId, string clientSecret)
    {
        var confidentialClientApplication = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithTenantId(tenantId)
            .WithClientSecret(clientSecret)
            .Build();

        var scopes = new[] { "https://graph.microsoft.com/.default" };

        var authenticationProvider = new ClientCredentialProvider(confidentialClientApplication);
        var httpClient = new HttpClient();

        return new GraphServiceClient(httpClient) { HttpProvider = new HttpProvider(httpClient, authenticationProvider) };
    }

    // Upload files to SharePoint
    static async Task<List<SharePointFile>> UploadToSharePoint(
        GraphServiceClient graphClient,
        string sharePointSiteUrl,
        string libraryName,
        List<BlobFile> blobFiles)
    {
        var uploadedFiles = new List<SharePointFile>();

        // Extract site ID from URL: https://tenant.sharepoint.com/sites/sitename → sitename
        var siteUrlParts = sharePointSiteUrl.Split('/');
        var siteName = siteUrlParts[siteUrlParts.Length - 1];
        var tenantName = siteUrlParts[2].Split('.')[0];

        Console.WriteLine($"   📍 Site: {siteName}");
        Console.WriteLine($"   📂 Library: {libraryName}\n");

        // Get site ID
        var site = await graphClient.Sites[$"{tenantName}.sharepoint.com:/sites/{siteName}"]
            .Request()
            .GetAsync();

        Console.WriteLine($"   ✅ Connected to site: {site.WebUrl}\n");

        // Get drive (document library)
        var drives = await graphClient.Sites[site.Id].Drives
            .Request()
            .GetAsync();

        var drive = drives.Value.FirstOrDefault(d => d.Name == libraryName);
        if (drive == null)
        {
            throw new Exception($"Document library '{libraryName}' not found in SharePoint site!");
        }

        Console.WriteLine($"   ✅ Found library: {libraryName}\n");

        // Upload each file
        int uploadCount = 0;
        foreach (var blobFile in blobFiles)
        {
            try
            {
                Console.WriteLine($"   ⏳ Uploading: {blobFile.Name}...");

                var uploadSession = await graphClient.Drives[drive.Id].Root.ItemWithPath(blobFile.Name)
                    .CreateUploadSession()
                    .Request()
                    .PostAsync();

                var maxChunkSize = 320 * 1024; // 320 KB chunks
                var largeUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, blobFile.Stream, maxChunkSize, graphClient);

                var uploadResult = await largeUploadTask.ResumeAsync();

                if (uploadResult.UploadSession == null)
                {
                    var uploadedItem = uploadResult.ItemResponse;
                    var fileUrl = $"{sharePointSiteUrl}/{libraryName}/{uploadedItem.Name}";

                    uploadedFiles.Add(new SharePointFile
                    {
                        Name = uploadedItem.Name,
                        Url = fileUrl
                    });

                    Console.WriteLine($"      ✅ Uploaded successfully!");
                    uploadCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Failed: {ex.Message}");
            }
        }

        Console.WriteLine($"\n   📊 Upload Summary: {uploadCount}/{blobFiles.Count} files uploaded");
        return uploadedFiles;
    }

    // Models
    class BlobFile
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public Stream Stream { get; set; }
    }

    class SharePointFile
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}