namespace SharePointMigration.Models;

public class MigrationReport
{
    public int TotalFilesDiscovered { get; set; }
    public int TotalFilesMigrated { get; set; }
    public int TotalFilesFailed { get; set; }
    public long TotalSizeBytes { get; set; }
    public Dictionary<string, int> FileTypeBreakdown { get; set; } = new();
    public List<FailedFileRecord> FailedFiles { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ExecutionId { get; set; }
}

public class FailedFileRecord
{
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }
    public string ErrorReason { get; set; }
    public int RetryAttempts { get; set; }
}