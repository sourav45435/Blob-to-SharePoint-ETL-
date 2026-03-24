namespace SharePointMigration;

public class MigrationEngine
{
    private readonly BlobStorageService _blobService;
    private readonly SharePointService _sharePointService;

    public MigrationEngine(BlobStorageService blobService, SharePointService sharePointService)
    {
        _blobService = blobService;
        _sharePointService = sharePointService;
    }

    public async Task MigrateFilesAsync()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    MIGRATION PROCESS STARTED                 ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

        // Step 1: Get files from blob
        Console.WriteLine("📥 STEP 1: Reading files from Azure Blob Storage");
        Console.WriteLine("───────────────────���─────────────────────────────\n");

        var blobFiles = await _blobService.GetFilesAsync(maxFiles: 2);

        if (blobFiles.Count == 0)
        {
            Console.WriteLine("❌ No files found in blob container!");
            return;
        }

        // Step 2: Download and upload
        Console.WriteLine("\n📤 STEP 2: Migrating files to SharePoint");
        Console.WriteLine("──────────────────────────────────────────\n");

        var results = new List<MigrationResult>();

        foreach (var blobFile in blobFiles)
        {
            Console.WriteLine($"Processing: {blobFile.Name}");

            try
            {
                // Download from blob
                Console.WriteLine($"   ⬇️  Downloading from Azure Blob...");
                var fileContent = await _blobService.DownloadFileAsync(blobFile.Name);
                Console.WriteLine($"      ✅ Downloaded ({fileContent.Length} bytes)");

                // Upload to SharePoint
                var uploadResult = await _sharePointService.UploadFileAsync(blobFile.Name, fileContent);

                if (uploadResult.Success)
                {
                    Console.WriteLine($"      ✅ Uploaded to SharePoint");
                    Console.WriteLine($"      🔗 URL: {uploadResult.Url}\n");

                    results.Add(new MigrationResult
                    {
                        FileName = blobFile.Name,
                        Status = "Success",
                        Url = uploadResult.Url,
                        Size = fileContent.Length
                    });
                }
                else
                {
                    Console.WriteLine($"      ❌ Upload failed: {uploadResult.Message}\n");

                    results.Add(new MigrationResult
                    {
                        FileName = blobFile.Name,
                        Status = "Failed",
                        Message = uploadResult.Message
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Error: {ex.Message}\n");

                results.Add(new MigrationResult
                {
                    FileName = blobFile.Name,
                    Status = "Failed",
                    Message = ex.Message
                });
            }
        }

        // Step 3: Generate report
        Console.WriteLine("\n📊 STEP 3: Generating Migration Report");
        Console.WriteLine("───────────────────────────────────────\n");

        GenerateReport(results);
    }

    private void GenerateReport(List<MigrationResult> results)
    {
        int successCount = results.Count(r => r.Status == "Success");
        int failureCount = results.Count(r => r.Status == "Failed");

        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    MIGRATION SUMMARY REPORT                  ║");
        Console.WriteLine("╚════════════════════════════════════��══════════════════════════╝\n");

        Console.WriteLine($"📈 Statistics:");
        Console.WriteLine($"   Total Files: {results.Count}");
        Console.WriteLine($"   ✅ Successful: {successCount}");
        Console.WriteLine($"   ❌ Failed: {failureCount}");
        Console.WriteLine($"   Success Rate: {(successCount * 100 / results.Count)}%\n");

        Console.WriteLine("📋 Details:\n");
        foreach (var result in results)
        {
            if (result.Status == "Success")
            {
                Console.WriteLine($"   ✅ {result.FileName}");
                Console.WriteLine($"      Size: {FormatBytes(result.Size)}");
                Console.WriteLine($"      URL: {result.Url}\n");
            }
            else
            {
                Console.WriteLine($"   ❌ {result.FileName}");
                Console.WriteLine($"      Error: {result.Message}\n");
            }
        }

        // Save report to file
        SaveReportToFile(results);
    }

    private void SaveReportToFile(List<MigrationResult> results)
    {
        Directory.CreateDirectory("reports");

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var reportPath = $"reports/migration_report_{timestamp}.csv";

        using (var writer = new StreamWriter(reportPath))
        {
            writer.WriteLine("FileName,Status,Size,URL,Message,Timestamp");

            foreach (var result in results)
            {
                var size = result.Size > 0 ? result.Size.ToString() : "N/A";
                var url = result.Url ?? "";
                var message = result.Message ?? "";

                writer.WriteLine($"\"{result.FileName}\",\"{result.Status}\",\"{size}\",\"{url}\",\"{message}\",\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"");
            }
        }

        Console.WriteLine($"📁 Report saved: {Path.GetFullPath(reportPath)}\n");
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

public class MigrationResult
{
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Url { get; set; }
    public long Size { get; set; }
    public string? Message { get; set; }
}