namespace SharePointMigration.Configuration;

public class SharePointOptions
{
    public string SiteUrl { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 300;
}