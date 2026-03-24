using CsvHelper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharePointMigration.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharePointMigration.Services;

public interface IReportingService
{
    Task GenerateCompletionReportAsync(MigrationReport report, List<BlobFileMetadata> files, CancellationToken cancellationToken = default);
}

public class ReportingService : IReportingService
{
    private readonly ILogger<ReportingService> _logger;
    private readonly string _reportDirectory;

    public ReportingService(ILogger<ReportingService> logger)
    {
        _logger = logger;
        _reportDirectory = Path.Combine(Directory.GetCurrentDirectory(), "reports");
        Directory.CreateDirectory(_reportDirectory);
    }

    public async Task GenerateCompletionReportAsync(
        MigrationReport report,
        List<BlobFileMetadata> files,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating migration completion reports...");

        await GenerateCsvReportAsync(report, files, cancellationToken);
        await GenerateJsonReportAsync(report, cancellationToken);
        await GenerateSummaryReportAsync(report, cancellationToken);
        await GenerateDetailedReportAsync(report, files, cancellationToken);
        await GenerateSharePointUrlsReportAsync(report, files, cancellationToken);

        _logger.LogInformation($"Reports generated in {_reportDirectory}");
    }

    private async Task GenerateCsvReportAsync(MigrationReport report, List<BlobFileMetadata> files, CancellationToken cancellationToken)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var csvPath = Path.Combine(_reportDirectory, $"migration_details_{timestamp}.csv");

        using (var writer = new StreamWriter(csvPath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteHeader<BlobFileMetadata>();
            await csv.NextRecordAsync();

            foreach (var file in files)
            {
                csv.WriteRecord(file);
                await csv.NextRecordAsync();
            }
        }

        _logger.LogInformation($"✓ CSV report: {csvPath}");
    }

    /// <summary>
    /// Generate a dedicated report with SharePoint URLs for each migrated file
    /// </summary>
    private async Task GenerateSharePointUrlsReportAsync(MigrationReport report, List<BlobFileMetadata> files, CancellationToken cancellationToken)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var urlReportPath = Path.Combine(_reportDirectory, $"sharepoint_urls_{timestamp}.csv");

        using (var writer = new StreamWriter(urlReportPath))
        {
            await writer.WriteLineAsync("FileName,SourcePath,TargetPath,SharePointUrl,FileSize,Status,MigratedDate");

            var successfulFiles = files.Where(f => f.Status == "Success").OrderBy(f => f.FileName);

            foreach (var file in successfulFiles)
            {
                var escapedUrl = file.SharePointUrl.Replace("\"", "\"\"");
                var line = $"\"{file.FileName}\",\"{file.BlobPath}\",\"{file.TransformedPath}\",\"{escapedUrl}\",{file.SizeBytes},\"{file.Status}\",\"{file.ProcessedDate:yyyy-MM-dd HH:mm:ss}\"";
                await writer.WriteLineAsync(line);
            }
        }

        _logger.LogInformation($"✓ SharePoint URLs report: {urlReportPath}");
    }

    private async Task GenerateJsonReportAsync(MigrationReport report, CancellationToken cancellationToken)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var jsonPath = Path.Combine(_reportDirectory, $"migration_summary_{timestamp}.json");
        
        var json = JsonConvert.SerializeObject(report, Formatting.Indented);
        await File.WriteAllTextAsync(jsonPath, json, cancellationToken);

        _logger.LogInformation($"✓ JSON summary: {jsonPath}");
    }

    private async Task GenerateSummaryReportAsync(MigrationReport report, CancellationToken cancellationToken)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var summaryPath = Path.Combine(_reportDirectory, $"migration_summary_{timestamp}.txt");

        var summary = new System.Text.StringBuilder();
        summary.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        summary.AppendLine("║       SHAREPOINT MIGRATION - EXECUTION REPORT                  ║");
        summary.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        summary.AppendLine();
        summary.AppendLine($"Execution ID:        {report.ExecutionId}");
        summary.AppendLine($"Environment:         {report.Environment}");
        summary.AppendLine($"Start Time:          {report.StartTime:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"End Time:            {report.EndTime:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"Total Duration:      {report.TotalDuration.TotalHours:F2} hours");
        summary.AppendLine();
        summary.AppendLine("─ SUMMARY ─");
        summary.AppendLine($"Files Discovered:    {report.TotalFilesDiscovered}");
        summary.AppendLine($"Files Migrated:      {report.TotalFilesMigrated} ({report.SuccessRate:F1}%)");
        summary.AppendLine($"Files Failed:        {report.TotalFilesFailed}");
        summary.AppendLine($"Files Skipped:       {report.TotalFilesDiscovered - report.TotalFilesMigrated - report.TotalFilesFailed}");
        summary.AppendLine($"Total Size:          {FormatBytes(report.TotalSizeBytes)}");
        summary.AppendLine($"Migrated Size:       {FormatBytes(report.MigratedSizeBytes)}");
        summary.AppendLine();
        summary.AppendLine("─ FILE TYPE BREAKDOWN ─");
        
        foreach (var kvp in report.FileTypeBreakdown.OrderByDescending(x => x.Value))
        {
            summary.AppendLine($"{kvp.Key,-10} : {kvp.Value,5} files");
        }

        if (report.FailedFiles.Count > 0)
        {
            summary.AppendLine();
            summary.AppendLine("─ FAILED FILES ─");
            foreach (var failed in report.FailedFiles.Take(20))
            {
                summary.AppendLine($"  Source:  {failed.SourcePath}");
                summary.AppendLine($"  Target:  {failed.TargetPath}");
                summary.AppendLine($"  Error:   {failed.ErrorReason}");
                summary.AppendLine($"  Retries: {failed.RetryAttempts}");
                summary.AppendLine();
            }

            if (report.FailedFiles.Count > 20)
            {
                summary.AppendLine($"  ... and {report.FailedFiles.Count - 20} more failures (see CSV for details)");
            }
        }

        summary.AppendLine();
        summary.AppendLine("╔════════════════════════════════════════════════════════════════╗");

        await File.WriteAllTextAsync(summaryPath, summary.ToString(), cancellationToken);
        _logger.LogInformation($"✓ Text summary: {summaryPath}");
    }

    private async Task GenerateDetailedReportAsync(MigrationReport report, List<BlobFileMetadata> files, CancellationToken cancellationToken)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var detailedPath = Path.Combine(_reportDirectory, $"migration_detailed_{timestamp}.txt");

        using (var writer = new StreamWriter(detailedPath))
        {
            await writer.WriteLineAsync("╔════════════════════════════════════════════════════════════════╗");
            await writer.WriteLineAsync("║       DETAILED MIGRATION REPORT WITH SHAREPOINT URLS           ║");
            await writer.WriteLineAsync("╚════════════════════════════════════════════════════════════════╝");
            await writer.WriteLineAsync();

            // Successful files with URLs
            var successful = files.Where(f => f.Status == "Success").ToList();
            await writer.WriteLineAsync($"✓ SUCCESSFUL MIGRATIONS ({successful.Count})");
            await writer.WriteLineAsync(new string('─', 100));
            
            foreach (var file in successful.Take(100))
            {
                await writer.WriteLineAsync($"File:     {file.FileName}");
                await writer.WriteLineAsync($"Size:     {FormatBytes(file.SizeBytes)}");
                await writer.WriteLineAsync($"Target:   {file.TransformedPath}");
                await writer.WriteLineAsync($"URL:      {file.SharePointUrl}");
                await writer.WriteLineAsync($"Date:     {file.ProcessedDate:yyyy-MM-dd HH:mm:ss}");
                await writer.WriteLineAsync();
            }
            
            if (successful.Count > 100)
                await writer.WriteLineAsync($"... and {successful.Count - 100} more\n");

            // Failed files
            var failed = files.Where(f => f.Status == "Failed").ToList();
            if (failed.Count > 0)
            {
                await writer.WriteLineAsync($"✗ FAILED MIGRATIONS ({failed.Count})");
                await writer.WriteLineAsync(new string('─', 100));
                foreach (var file in failed)
                {
                    await writer.WriteLineAsync($"File:     {file.FileName}");
                    await writer.WriteLineAsync($"Source:   {file.BlobPath}");
                    await writer.WriteLineAsync($"Error:    {file.ErrorMessage}");
                    await writer.WriteLineAsync();
                }
            }
        }

        _logger.LogInformation($"✓ Detailed report: {detailedPath}");
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