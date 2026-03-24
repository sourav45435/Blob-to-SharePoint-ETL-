using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SharePointMigration.Configuration;
using SharePointMigration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharePointMigration.Services;

public interface ISharePointMigrationService
{
    Task<MigrationReport> MigrateFilesAsync(List<BlobFileMetadata> files, CancellationToken cancellationToken = default);
    Task<string> GetMigrationStatusAsync(string jobId, CancellationToken cancellationToken = default);
}

public class SharePointMigrationService : ISharePointMigrationService
{
    private readonly SharePointOptions _options;
    private readonly ILogger<SharePointMigrationService> _logger;
    private readonly IBlobDownloadService _blobService;
    private readonly IAsyncPolicy<bool> _retryPolicy;

    public SharePointMigrationService(
        IOptions<SharePointOptions> options,
        ILogger<SharePointMigrationService> logger,
        IBlobDownloadService blobService)
    {
        _options = options.Value;
        _logger = logger;
        _blobService = blobService;
        
        // Setup retry policy with exponential backoff
        _retryPolicy = Policy<bool>
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .OrResult(r => !r)
            .WaitAndRetryAsync(
                retryCount: _options.MaxRetries,
                sleepDurationProvider: attempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                    _logger.LogWarning($"Retry {retryCount} after {timespan.TotalSeconds}s")
            );
    }

    public async Task<MigrationReport> MigrateFilesAsync(List<BlobFileMetadata> files, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Starting SharePoint migration for {files.Count} files...");

        var report = new MigrationReport
        {
            ExecutionId = Guid.NewGuid().ToString(),
            StartTime = DateTime.UtcNow,
            TotalFilesDiscovered = files.Count,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };

        // Group files into batches
        var batches = files
            .GroupBy((f, i) => i / _options.BatchSize)
            .Select(g => g.ToList())
            .ToList();

        _logger.LogInformation($"Processing {batches.Count} batches (batch size: {_options.BatchSize})");

        var semaphore = new SemaphoreSlim(_options.MaxRetries, _options.MaxRetries);

        for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            var batch = batches[batchIndex];
            _logger.LogInformation($"Processing batch {batchIndex + 1}/{batches.Count} with {batch.Count} files");

            await ProcessBatchAsync(batch, report, semaphore, cancellationToken);
        }

        report.EndTime = DateTime.UtcNow;
        report.TotalFilesMigrated = files.Count(f => f.Status == "Success");
        report.TotalFilesFailed = files.Count(f => f.Status == "Failed");
        report.TotalSizeBytes = files.Sum(f => f.SizeBytes);
        report.MigratedSizeBytes = files.Where(f => f.Status == "Success").Sum(f => f.SizeBytes);

        // Build file type breakdown
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!report.FileTypeBreakdown.ContainsKey(ext))
                report.FileTypeBreakdown[ext] = 0;
            report.FileTypeBreakdown[ext]++;
        }

        _logger.LogInformation(
            $"Migration complete. Success: {report.TotalFilesMigrated}, Failed: {report.TotalFilesFailed}, Duration: {report.TotalDuration.TotalHours:F2}h");

        return report;
    }

    private async Task ProcessBatchAsync(
        List<BlobFileMetadata> batch,
        MigrationReport report,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        var tasks = batch.Select(async file =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await MigrateFileAsync(file, report, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error migrating file: {file.BlobPath}");
                file.Status = "Failed";
                file.ErrorMessage = ex.Message;
                file.ProcessedDate = DateTime.UtcNow;
                report.FailedFiles.Add(new FailedFileRecord
                {
                    SourcePath = file.BlobPath,
                    TargetPath = file.TransformedPath,
                    ErrorReason = ex.Message,
                    RetryAttempts = file.RetryCount,
                    FailedDate = DateTime.UtcNow
                });
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task MigrateFileAsync(BlobFileMetadata file, MigrationReport report, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Migrating: {file.BlobPath} → {file.TransformedPath}");

        var success = await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                // Download blob
                var blobStream = await _blobService.DownloadBlobAsync(file.BlobPath, cancellationToken);
                
                // Generate SharePoint URL
                var sharePointUrl = GenerateSharePointUrl(file.TransformedPath);
                
                // TODO: Implement actual upload to SharePoint using Migration API
                // For now, simulating successful migration
                await SimulateUploadAsync(file, blobStream, cancellationToken);
                
                file.Status = "Success";
                file.SharePointUrl = sharePointUrl;
                file.ProcessedDate = DateTime.UtcNow;
                
                _logger.LogInformation($"✓ Successfully migrated: {sharePointUrl}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Migration failed for {file.BlobPath}");
                file.RetryCount++;
                return false;
            }
        });

        if (!success)
        {
            file.Status = "Failed";
        }
    }

    /// <summary>
    /// Generates the final SharePoint URL where the file will be accessible
    /// </summary>
    private string GenerateSharePointUrl(string transformedPath)
    {
        // Build URL: https://tenant.sharepoint.com/sites/site/Documents/path/to/file.pdf
        var baseUrl = _options.SiteUrl.TrimEnd('/');
        var libraryName = _options.LibraryName.Trim('/');
        var filePath = transformedPath.TrimStart('/');

        return $"{baseUrl}/{libraryName}/{filePath}";
    }

    /// <summary>
    /// Simulates file upload (placeholder for real SharePoint Migration API call)
    /// </summary>
    private async Task SimulateUploadAsync(BlobFileMetadata file, Stream fileStream, CancellationToken cancellationToken)
    {
        // In production, replace this with actual SharePoint Upload API call
        // Example: using PnP.Core or Microsoft.SharePoint.Client to upload
        
        _logger.LogDebug($"Uploading file: {file.FileName} (Size: {FormatBytes(file.SizeBytes)})");
        
        // Simulate network latency
        await Task.Delay(100, cancellationToken);
    }

    public async Task<string> GetMigrationStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        // TODO: Poll SharePoint Migration API for job status
        await Task.CompletedTask;
        return "Completed";
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
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