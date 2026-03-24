using SharePointMigration;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Azure Blob → SharePoint Online File Migration             ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

        try
        {
            // Load configuration
            var config = Config.LoadFromJson("appsettings.json");
            
            if (!config.IsValid())
            {
                Console.WriteLine("❌ Invalid configuration. Please update appsettings.json");
                return;
            }

            Console.WriteLine($"📋 Configuration loaded successfully\n");

            // Initialize services
            var blobService = new BlobStorageService(config.BlobConnectionString, config.BlobContainerName);
            var sharePointService = new SharePointService(config.SharePointSiteUrl, config.SharePointLibraryName);
            
            // Run migration
            var engine = new MigrationEngine(blobService, sharePointService);
            await engine.MigrateFilesAsync();

            Console.WriteLine("\n✅ Migration completed successfully!");
            Console.WriteLine("📂 Check the 'reports' folder for detailed logs.\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ERROR: {ex.Message}");
            Console.WriteLine($"📋 Details: {ex.InnerException?.Message}\n");
        }
    }
}