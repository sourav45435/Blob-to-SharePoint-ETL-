namespace SharePointMigration.Models;

public class MigrationReport
{
    public int TotalFilesDiscovered { get; set; }
    public int TotalFilesMigrated { get; set; }
    public int TotalFilesFailed { get; set; }
    public long TotalSizeBytes { get; set; }
    public long MigratedSizeBytes { get; set; }
    public Dictionary<string, int> FileTypeBreakdown { get; set; } = new();
    public List<FailedFileRecord> FailedFiles { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ExecutionId { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;

    public TimeSpan TotalDuration => EndTime - StartTime;
    public double SuccessRate => TotalFilesDiscovered > 0 
        ? (TotalFilesMigrated / (double)TotalFilesDiscovered) * 100 
        : 0;
    public double SkippedRate => TotalFilesDiscovered > 0
        ? ((TotalFilesDiscovered - TotalFilesMigrated - TotalFilesFailed) / (double)TotalFilesDiscovered) * 100
        : 0;
}

public class FailedFileRecord
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string ErrorReason { get; set; } = string.Empty;
    public int RetryAttempts { get; set; }
    public DateTime FailedDate { get; set; }
}