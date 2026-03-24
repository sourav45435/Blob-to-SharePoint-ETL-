using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharePointMigration.Configuration;
using SharePointMigration.Models;

namespace SharePointMigration.Services;

public interface IBlobInventoryService
{
    Task<List<BlobFileMetadata>> GetAllBlobsAsync(CancellationToken cancellationToken = default);
}

public class BlobInventoryService : IBlobInventoryService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobInventoryService> _logger;
    private readonly string[] _supportedExtensions = 
    { 
        ".pdf", ".csv", ".html", ".txt", ".docx", ".xlsx", 
        ".pptx", ".json", ".xml", ".log", ".rtf" 
    };

    public BlobInventoryService(
        IOptions<AzureBlobOptions> options,
        ILogger<BlobInventoryService> logger)
    {
        var blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
        _logger = logger;
    }

    public async Task<List<BlobFileMetadata>> GetAllBlobsAsync(CancellationToken cancellationToken = default)
    {
        var blobs = new List<BlobFileMetadata>();

        _logger.LogInformation("Starting blob inventory enumeration...");

        try
        {
            await foreach (var blobItem in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                // Filter by supported document types
                var extension = Path.GetExtension(blobItem.Name).ToLowerInvariant();
                if (!_supportedExtensions.Contains(extension))
                {
                    _logger.LogDebug($"Skipping unsupported file type: {blobItem.Name}");
                    continue;
                }

                var properties = blobItem.Properties;

                blobs.Add(new BlobFileMetadata
                {
                    BlobPath = blobItem.Name,
                    FileName = Path.GetFileName(blobItem.Name),
                    SizeBytes = properties.ContentLength ?? 0,
                    CreatedDate = properties.CreatedOn ?? DateTimeOffset.UtcNow,
                    ModifiedDate = properties.LastModified ?? DateTimeOffset.UtcNow,
                    ContentType = properties.ContentType ?? "application/octet-stream",
                    Status = "Discovered"
                });
            }

            _logger.LogInformation($"Inventory complete: {blobs.Count} eligible files discovered. Total size: {FormatBytes(blobs.Sum(b => b.SizeBytes))}");
            return blobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during blob inventory enumeration");
            throw;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}