using Newtonsoft.Json;

namespace SharePointMigration.Models;

public class PathMapping
{
    [JsonProperty("sourcePrefix")]
    public string SourcePrefix { get; set; } = string.Empty;

    [JsonProperty("targetPrefix")]
    public string TargetPrefix { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;
}