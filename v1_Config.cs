using Newtonsoft.Json;

namespace SharePointMigration;

public class Config
{
    [JsonProperty("azureBlob")]
    public AzureBlobConfig AzureBlob { get; set; } = new();

    [JsonProperty("sharePoint")]
    public SharePointConfig SharePoint { get; set; } = new();

    [JsonProperty("migration")]
    public MigrationConfig Migration { get; set; } = new();

    // Convenience properties
    public string BlobConnectionString => AzureBlob.ConnectionString;
    public string BlobContainerName => AzureBlob.ContainerName;
    public string SharePointSiteUrl => SharePoint.SiteUrl;
    public string SharePointLibraryName => SharePoint.LibraryName;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(BlobConnectionString) &&
               !string.IsNullOrWhiteSpace(BlobContainerName) &&
               !string.IsNullOrWhiteSpace(SharePointSiteUrl) &&
               !string.IsNullOrWhiteSpace(SharePointLibraryName);
    }

    public static Config LoadFromJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<Config>(json) ?? new Config();
    }
}

public class AzureBlobConfig
{
    [JsonProperty("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;

    [JsonProperty("containerName")]
    public string ContainerName { get; set; } = string.Empty;

    [JsonProperty("maxFiles")]
    public int MaxFiles { get; set; } = 2;
}

public class SharePointConfig
{
    [JsonProperty("siteUrl")]
    public string SiteUrl { get; set; } = string.Empty;

    [JsonProperty("libraryName")]
    public string LibraryName { get; set; } = string.Empty;
}

public class MigrationConfig
{
    [JsonProperty("enableLocalSave")]
    public bool EnableLocalSave { get; set; } = true;

    [JsonProperty("outputFolder")]
    public string OutputFolder { get; set; } = "migrated_files";

    [JsonProperty("enableReporting")]
    public bool EnableReporting { get; set; } = true;
}