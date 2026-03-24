namespace SharePointMigration.Configuration;

public class MigrationOptions
{
    public string PathMappingsFile { get; set; }
    public int ConcurrencyLevel { get; set; } = 5;
    public bool PreserveModificationDates { get; set; } = true;
    public string MetadataColumnsFile { get; set; }
}