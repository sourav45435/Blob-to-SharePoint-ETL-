using Microsoft.Extensions.Logging;
using SharePointMigration.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharePointMigration.Services;

public interface IETLOrchestrator
{
    Task ExecuteAsync();
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

    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("ETL Pipeline Starting...");

            // Step 1: Inventory
            _logger.LogInformation("Step 1/4: Reading blob inventory...");
            var files = await _blobService.GetAllBlobsAsync();

            if (files.Count == 0)
            {
                _logger.LogWarning("No eligible files found in blob container. Exiting.");
                return;
            }

            // Step 2: Transform
            _logger.LogInformation("Step 2/4: Transforming paths...");
            await _transformationService.TransformPathsAsync(files);

            // Step 3: Migrate
            _logger.LogInformation("Step 3/4: Executing SharePoint migration...");
            var report = await _migrationService.MigrateFilesAsync(files);

            // Step 4: Report
            _logger.LogInformation("Step 4/4: Generating reports...");
            await _reportingService.GenerateCompletionReportAsync(report, files);

            _logger.LogInformation("ETL Pipeline Complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ETL Pipeline failed");
            throw;
        }
    }
}