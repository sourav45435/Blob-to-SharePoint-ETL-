namespace SharePointMigration.Configuration;

public class SharePointOptions
{
    public string SiteUrl { get; set; }
    public string LibraryName { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string TenantId { get; set; }
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 3;
}