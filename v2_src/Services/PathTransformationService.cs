using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SharePointMigration.Configuration;
using SharePointMigration.Models;
using System.Text.RegularExpressions;

namespace SharePointMigration.Services;

public interface IPathTransformationService
{
    Task TransformPathsAsync(List<BlobFileMetadata> files, CancellationToken cancellationToken = default);
}

public class PathTransformationService : IPathTransformationService
{
    private readonly ILogger<PathTransformationService> _logger;
    private readonly string _mappingsFile;
    private List<PathMapping> _mappings = new();

    public PathTransformationService(
        IOptions<MigrationOptions> options,
        ILogger<PathTransformationService> logger)
    {
        _logger = logger;
        _mappingsFile = options.Value.PathMappingsFile;
    }

    public async Task TransformPathsAsync(List<BlobFileMetadata> files, CancellationToken cancellationToken = default)
    {
        await LoadMappingsAsync();

        _logger.LogInformation("Starting path transformation...");

        foreach (var file in files)
        {
            var originalPath = file.BlobPath;
            var transformedPath = ApplyMappings(originalPath);

            file.TransformedPath = transformedPath;
            file.TransformedFileName = Path.GetFileName(transformedPath);

            _logger.LogDebug($"Transformation: {originalPath} → {transformedPath}");
        }

        _logger.LogInformation($"Path transformation complete. {files.Count} files transformed.");
    }

    private async Task LoadMappingsAsync()
    {
        try
        {
            if (!File.Exists(_mappingsFile))
            {
                _logger.LogWarning($"Path mappings file not found: {_mappingsFile}. Using default (no transformation).");
                _mappings = new List<PathMapping>();
                return;
            }

            var json = await File.ReadAllTextAsync(_mappingsFile);
            var mappingContainer = JsonConvert.DeserializeObject<MappingContainer>(json);
            _mappings = mappingContainer?.Mappings?.Where(m => m.Enabled).ToList() ?? new List<PathMapping>();
            
            _logger.LogInformation($"Loaded {_mappings.Count} active path mapping rules.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading path mappings from {_mappingsFile}");
            _mappings = new List<PathMapping>();
        }
    }

    private string ApplyMappings(string blobPath)
    {
        var normalizedPath = blobPath.Replace("\\", "/");

        foreach (var mapping in _mappings)
        {
            var sourcePattern = mapping.SourcePrefix
                .Replace("*", ".*")
                .Replace(".", "\\.")
                .TrimEnd('/');

            if (Regex.IsMatch(normalizedPath, $"^{sourcePattern}", RegexOptions.IgnoreCase))
            {
                var prefix = mapping.SourcePrefix.TrimEnd('/', '*').TrimEnd('/');
                var remainder = normalizedPath.Substring(prefix.Length).TrimStart('/');
                var targetPath = mapping.TargetPrefix.TrimEnd('/') + "/" + remainder;
                return NormalizePath(targetPath);
            }
        }

        return normalizedPath;
    }

    private static string NormalizePath(string path)
    {
        path = Regex.Replace(path, @"/+", "/");
        return path.Trim('/');
    }

    private class MappingContainer
    {
        [JsonProperty("mappings")]
        public List<PathMapping> Mappings { get; set; } = new();
    }
}