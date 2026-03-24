using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PnP.Core;
using PnP.Core.Auth;
using SharePointMigration.Configuration;
using SharePointMigration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharePointMigration.Services;

public interface ISharePointMigrationService
{
    Task<MigrationReport> MigrateFilesAsync(List<BlobFileMetadata> files);
    Task<string> GetMigrationStatusAsync(string jobId);
}

public class SharePointMigrationService : ISharePointMigrationService
{
    private readonly SharePointOptions _options;
    private readonly ILogger<SharePointMigrationService> _logger;
    private readonly IBlobDownloadService _blobService;

    public SharePointMigrationService(
        IOptions<SharePointOptions> options,
        ILogger<SharePointMigrationService> logger,
        IBlobDownloadService blobService)
    {
        _options = options.Value;
        _logger = logger;
        _blobService = blobService;
    }

    public async Task<MigrationReport> MigrateFilesAsync(List<BlobFileMetadata> files)
    {
        _logger.LogInformation($"Starting SharePoint migration for {files.Count} files...");

        var report = new MigrationReport
        {
            ExecutionId = Guid.NewGuid().ToString(),
            StartTime = DateTime.UtcNow,
            TotalFilesDiscovered = files.Count
        };

        // Group files into batches
        var batches = files
            .GroupBy((f, i) => i / _options.BatchSize)
            .Select(g => g.ToList())
            .ToList();

        _logger.LogInformation($"Processing {batches.Count} batches (size: {_options.BatchSize})");

        for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            var batch = batches[batchIndex];
            _logger.LogInformation($"Processing batch {batchIndex + 1}/{batches.Count}");

            await ProcessBatchAsync(batch, report);
        }

        report.EndTime = DateTime.UtcNow;
        report.TotalFilesMigrated = files.Count(f => f.Status == "Success");
        report.TotalFilesFailed = files.Count(f => f.Status == "Failed");
        report.TotalSizeBytes = files.Sum(f => f.SizeBytes);

        // Build file type breakdown
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!report.FileTypeBreakdown.ContainsKey(ext))
                report.FileTypeBreakdown[ext] = 0;
            report.FileTypeBreakdown[ext]++;
        }

        _logger.LogInformation($"Migration complete. Success: {report.TotalFilesMigrated}, Failed: {report.TotalFilesFailed}");
        return report;
    }

    private async Task ProcessBatchAsync(List<BlobFileMetadata> batch, MigrationReport report)
    {
        var tasks = batch.Select(async file =>
        {
            try
            {
                await MigrateFileAsync(file, report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error migrating file: {file.BlobPath}");
                file.Status = "Failed";
                file.ErrorMessage = ex.Message;
                report.FailedFiles.Add(new FailedFileRecord
                {
                    SourcePath = file.BlobPath,
                    TargetPath = file.TransformedPath,
                    ErrorReason = ex.Message,
                    RetryAttempts = file.RetryCount
                });
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task MigrateFileAsync(BlobFileMetadata file, MigrationReport report)
    {
        _logger.LogInformation($"Migrating: {file.BlobPath} → {file.TransformedPath}");

        // TODO: Implement actual SharePoint upload
        // This is a placeholder for the real Migration API integration
        
        // Download blob to stream
        var blobStream = await _blobService.DownloadBlobAsync(file.BlobPath);

        // Create folder structure in SharePoint if needed
        await EnsureSharePointFolderStructureAsync(file.TransformedPath);

        // Upload file with metadata
        var spUrl = await UploadFileToSharePointAsync(file, blobStream);

        file.Status = "Success";
        file.SharePointUrl = spUrl;
        _logger.LogInformation($"Successfully migrated: {spUrl}");
    }

    private async Task EnsureSharePointFolderStructureAsync(string targetPath)
    {
        var folderPath = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrEmpty(folderPath)) return;

        // TODO: Use PnP Core to create folder structure
        await Task.CompletedTask;
    }

    private async Task<string> UploadFileToSharePointAsync(BlobFileMetadata file, Stream fileStream)
    {
        // TODO: Use PnP Core to upload file and set metadata
        var mockUrl = $"{_options.SiteUrl}/sites/{_options.LibraryName}/{file.TransformedPath}";
        await Task.CompletedTask;
        return mockUrl;
    }

    public async Task<string> GetMigrationStatusAsync(string jobId)
    {
        // TODO: Implement job status polling
        await Task.CompletedTask;
        return "Completed";
    }
}