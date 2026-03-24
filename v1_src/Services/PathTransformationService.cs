using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SharePointMigration.Configuration;
using SharePointMigration.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharePointMigration.Services;

public interface IPathTransformationService
{
    Task TransformPathsAsync(List<BlobFileMetadata> files);
}

public class PathTransformationService : IPathTransformationService
{
    private readonly ILogger<PathTransformationService> _logger;
    private List<PathMapping> _mappings;

    public PathTransformationService(
        IOptions<MigrationOptions> options,
        ILogger<PathTransformationService> logger)
    {
        _logger = logger;
        LoadMappings(options.Value.PathMappingsFile);
    }

    private void LoadMappings(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var mappingContainer = JsonConvert.DeserializeObject<MappingContainer>(json);
            _mappings = mappingContainer?.Mappings ?? new List<PathMapping>();
            _logger.LogInformation($"Loaded {_mappings.Count} path mapping rules.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading path mappings from {filePath}");
            _mappings = new List<PathMapping>();
        }
    }

    public async Task TransformPathsAsync(List<BlobFileMetadata> files)
    {
        _logger.LogInformation("Starting path transformation...");

        foreach (var file in files)
        {
            var originalPath = file.BlobPath;
            var transformedPath = ApplyMappings(originalPath);

            file.TransformedPath = transformedPath;
            file.TransformedFileName = Path.GetFileName(transformedPath);

            _logger.LogDebug($"Transformation: {originalPath} → {transformedPath}");
        }

        _logger.LogInformation("Path transformation complete.");
        await Task.CompletedTask;
    }

    private string ApplyMappings(string blobPath)
    {
        // Normalize path separators
        var normalizedPath = blobPath.Replace("\\", "/");

        foreach (var mapping in _mappings)
        {
            // Handle wildcard patterns
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

        // Default: preserve original structure
        return normalizedPath;
    }

    private string NormalizePath(string path)
    {
        // Remove leading/trailing slashes, collapse multiple slashes
        path = Regex.Replace(path, @"/+", "/");
        return path.Trim('/');
    }

    private class MappingContainer
    {
        [JsonProperty("mappings")]
        public List<PathMapping> Mappings { get; set; }
    }
}