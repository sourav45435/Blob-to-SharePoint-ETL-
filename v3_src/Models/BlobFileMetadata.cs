namespace SharePointMigration.Models;

public class BlobFileMetadata
{
    public string BlobPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset ModifiedDate { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string TransformedPath { get; set; } = string.Empty;
    public string TransformedFileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string SharePointUrl { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public string SourceHash { get; set; } = string.Empty;
    public DateTime ProcessedDate { get; set; }
}