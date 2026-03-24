using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SharePointMigration.Configuration;
using SharePointMigration.Services;
using System.Threading.Tasks;

namespace SharePointMigration;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                "logs/migration-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("=== SharePoint Migration ETL Application Started ===");

            var host = CreateHostBuilder(args).Build();
            var etlOrchestrator = host.Services.GetRequiredService<IETLOrchestrator>();

            await etlOrchestrator.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.Information("=== SharePoint Migration ETL Application Stopped ===");
            await Log.CloseAndFlushAsync();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<AzureBlobOptions>(
                    context.Configuration.GetSection("AzureBlob"));
                services.Configure<SharePointOptions>(
                    context.Configuration.GetSection("SharePoint"));
                services.Configure<MigrationOptions>(
                    context.Configuration.GetSection("Migration"));

                // Services
                services.AddScoped<IBlobInventoryService, BlobInventoryService>();
                services.AddScoped<IPathTransformationService, PathTransformationService>();
                services.AddScoped<IBlobDownloadService, BlobDownloadService>();
                services.AddScoped<ISharePointMigrationService, SharePointMigrationService>();
                services.AddScoped<IReportingService, ReportingService>();
                services.AddScoped<IETLOrchestrator, ETLOrchestrator>();
            });
}