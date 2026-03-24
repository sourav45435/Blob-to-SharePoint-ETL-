using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SharePointMigration.Configuration;
using SharePointMigration.Services;

namespace SharePointMigration;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithMachineName()
            .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/migration-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30)
            .CreateLogger();

        try
        {
            Log.Information("═══════════════════════════════════════════════════════════════════════");
            Log.Information("SharePoint Migration ETL Application - Startup");
            Log.Information("═══════════════════════════════════════════════════════════════════════");

            var host = CreateHostBuilder(args).Build();
            var etlOrchestrator = host.Services.GetRequiredService<IETLOrchestrator>();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Log.Warning("Shutdown signal received. Gracefully stopping...");
                cts.Cancel();
            };

            await etlOrchestrator.ExecuteAsync(cts.Token);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            Environment.Exit(1);
        }
        finally
        {
            Log.Information("═══════════════════════════════════════════════════════════════════════");
            Log.Information("SharePoint Migration ETL Application - Shutdown");
            Log.Information("═══════════════════════════════════════════════════════════════════════");
            await Log.CloseAndFlushAsync();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true)
                    .AddEnvironmentVariables(prefix: "SP_")
                    .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<AzureBlobOptions>(context.Configuration.GetSection("AzureBlob"));
                services.Configure<SharePointOptions>(context.Configuration.GetSection("SharePoint"));
                services.Configure<MigrationOptions>(context.Configuration.GetSection("Migration"));

                // Services
                services.AddScoped<IBlobInventoryService, BlobInventoryService>();
                services.AddScoped<IPathTransformationService, PathTransformationService>();
                services.AddScoped<IBlobDownloadService, BlobDownloadService>();
                services.AddScoped<ISharePointMigrationService, SharePointMigrationService>();
                services.AddScoped<IReportingService, ReportingService>();
                services.AddScoped<IETLOrchestrator, ETLOrchestrator>();

                // HTTP client for future SharePoint API calls
                services.AddHttpClient();
            });
}