using CsvHelper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharePointMigration.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace SharePointMigration.Services;

public interface IReportingService
{
    Task GenerateCompletionReportAsync(MigrationReport report, List<BlobFileMetadata> files);
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

    public async Task GenerateCompletionReportAsync(MigrationReport report, List<BlobFileMetadata> files)
    {
        _logger.LogInformation("Generating migration completion report...");

        // CSV Report
        await GenerateCsvReportAsync(report, files);

        // JSON Report
        await GenerateJsonReportAsync(report);

        // Summary Report
        await GenerateSummaryReportAsync(report);

        _logger.LogInformation($"Reports generated in {_reportDirectory}");
    }

    private async Task GenerateCsvReportAsync(MigrationReport report, List<BlobFileMetadata> files)
    {
        var csvPath = Path.Combine(_reportDirectory, $"migration_report_{report.ExecutionId}.csv");

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

        _logger.LogInformation($"CSV report written: {csvPath}");
    }

    private async Task GenerateJsonReportAsync(MigrationReport report)
    {
        var jsonPath = Path.Combine(_reportDirectory, $"migration_summary_{report.ExecutionId}.json");
        var json = JsonConvert.SerializeObject(report, Formatting.Indented);

        await File.WriteAllTextAsync(jsonPath, json);
        _logger.LogInformation($"JSON report written: {jsonPath}");
    }

    private async Task GenerateSummaryReportAsync(MigrationReport report)
    {
        var summaryPath = Path.Combine(_reportDirectory, $"migration_summary_{report.ExecutionId}.txt");

        var summary = new System.Text.StringBuilder();
        summary.AppendLine("=== SHAREPOINT MIGRATION REPORT ===");
        summary.AppendLine($"Execution ID: {report.ExecutionId}");
        summary.AppendLine($"Start Time: {report.StartTime:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"End Time: {report.EndTime:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"Duration: {(report.EndTime - report.StartTime).TotalHours:F2} hours");
        summary.AppendLine();
        summary.AppendLine("--- Summary ---");
        summary.AppendLine($"Total Files Discovered: {report.TotalFilesDiscovered}");
        summary.AppendLine($"Files Successfully Migrated: {report.TotalFilesMigrated}");
        summary.AppendLine($"Files Failed: {report.TotalFilesFailed}");
        summary.AppendLine($"Total Size: {FormatBytes(report.TotalSizeBytes)}");
        summary.AppendLine();
        summary.AppendLine("--- File Type Breakdown ---");
        foreach (var kvp in report.FileTypeBreakdown)
        {
            summary.AppendLine($"{kvp.Key}: {kvp.Value} files");
        }

        if (report.FailedFiles.Count > 0)
        {
            summary.AppendLine();
            summary.AppendLine("--- Failed Files ---");
            foreach (var failed in report.FailedFiles)
            {
                summary.AppendLine($"Source: {failed.SourcePath}");
                summary.AppendLine($"Target: {failed.TargetPath}");
                summary.AppendLine($"Error: {failed.ErrorReason}");
                summary.AppendLine($"Retries: {failed.RetryAttempts}");
                summary.AppendLine();
            }
        }

        await File.WriteAllTextAsync(summaryPath, summary.ToString());
        _logger.LogInformation($"Summary report written: {summaryPath}");
    }

    private string FormatBytes(long bytes)
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