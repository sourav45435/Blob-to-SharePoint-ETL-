using Microsoft.Extensions.Logging;
using SharePointMigration.Models;

namespace SharePointMigration.Services;

public interface IETLOrchestrator
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public class ETLOrchestrator : IETLOrchestrator
{
    private readonly IBlobInventoryService _blobService;
    private readonly IPathTransformationService _transformationService;
    private readonly ISharePointMigrationService _migrationService;
    private readonly IReportingService _reportingService;
    private readonly ILogger<ETLOrchestrator> _logger;

    public ETLOrchestrator(
        IBlobInventoryService blobService,
        IPathTransformationService transformationService,
        ISharePointMigrationService migrationService,
        IReportingService reportingService,
        ILogger<ETLOrchestrator> logger)
    {
        _blobService = blobService;
        _transformationService = transformationService;
        _migrationService = migrationService;
        _reportingService = reportingService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _logger.LogInformation("╔════════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║     Azure Blob → SharePoint Online ETL Pipeline Started       ║");
            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");
            _logger.LogInformation("");

            // Step 1: Inventory
            _logger.LogInformation("Step 1/4: Reading blob inventory...");
            var files = await _blobService.GetAllBlobsAsync(cancellationToken);

            if (files.Count == 0)
            {
                _logger.LogWarning("⚠ No eligible files found in blob container. Pipeline completed with no files processed.");
                return;
            }

            // Step 2: Transform
            _logger.LogInformation("Step 2/4: Transforming paths according to mapping rules...");
            await _transformationService.TransformPathsAsync(files, cancellationToken);

            // Step 3: Migrate
            _logger.LogInformation("Step 3/4: Executing SharePoint migration...");
            var report = await _migrationService.MigrateFilesAsync(files, cancellationToken);

            // Step 4: Report
            _logger.LogInformation("Step 4/4: Generating completion reports...");
            await _reportingService.GenerateCompletionReportAsync(report, files, cancellationToken);

            _logger.LogInformation("");
            _logger.LogInformation("╔════════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║              ETL Pipeline Completed Successfully              ║");
            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");
            _logger.LogInformation($"Total Execution Time: {(DateTime.UtcNow - startTime).TotalHours:F2} hours");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ETL Pipeline was cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ETL Pipeline failed with critical error");
            throw;
        }
    }
}