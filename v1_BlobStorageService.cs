using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SharePointMigration;

public class BlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly string _containerName;

    public BlobStorageService(string connectionString, string containerName)
    {
        _containerName = containerName;
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<List<BlobFileInfo>> GetFilesAsync(int maxFiles = 2)
    {
        Console.WriteLine($"🔍 Reading files from blob container '{_containerName}'...\n");

        var files = new List<BlobFileInfo>();
        int count = 0;

        try
        {
            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync())
            {
                if (count >= maxFiles)
                    break;

                files.Add(new BlobFileInfo
                {
                    Name = blobItem.Name,
                    Size = blobItem.Properties.ContentLength ?? 0,
                    Created = blobItem.Properties.CreatedOn?.DateTime ?? DateTime.MinValue,
                    Modified = blobItem.Properties.LastModified?.DateTime ?? DateTime.MinValue
                });

                count++;
            }

            Console.WriteLine($"✅ Found {files.Count} file(s) in blob container:\n");
            foreach (var file in files)
            {
                Console.WriteLine($"   📄 {file.Name}");
                Console.WriteLine($"      Size: {FormatBytes(file.Size)}");
                Console.WriteLine($"      Created: {file.Created:yyyy-MM-dd HH:mm:ss}\n");
            }

            return files;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to read files from blob storage: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> DownloadFileAsync(string fileName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            var download = await blobClient.DownloadAsync();

            using (var ms = new MemoryStream())
            {
                await download.Value.Content.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download file '{fileName}': {ex.Message}", ex);
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class BlobFileInfo
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}