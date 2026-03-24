namespace SharePointMigration.Configuration;

public class AzureBlobOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public int MaxDegreeOfParallelism { get; set; } = 5;
}