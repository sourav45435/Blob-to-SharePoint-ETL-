namespace SharePointMigration.Models;

public class BlobFileMetadata
{
    public string BlobPath { get; set; }
    public string FileName { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset ModifiedDate { get; set; }
    public string ContentType { get; set; }
    public string TransformedPath { get; set; }
    public string TransformedFileName { get; set; }
    public string Status { get; set; } = "Pending";
    public string SharePointUrl { get; set; }
    public string ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}