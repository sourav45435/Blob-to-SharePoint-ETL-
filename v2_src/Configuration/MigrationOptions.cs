namespace SharePointMigration.Configuration;

public class MigrationOptions
{
    public string PathMappingsFile { get; set; } = "pathMappings.json";
    public int ConcurrencyLevel { get; set; } = 5;
    public bool PreserveModificationDates { get; set; } = true;
    public bool DryRun { get; set; } = false;
    public string ManifestOutputPath { get; set; } = "./manifests";
    public int MaxFileSizePerBatch { get; set; } = 1048576000; // 1GB
}