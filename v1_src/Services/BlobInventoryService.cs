using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharePointMigration.Configuration;
using SharePointMigration.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharePointMigration.Services;

public interface IBlobInventoryService
{
    Task<List<BlobFileMetadata>> GetAllBlobsAsync();
}

public class BlobInventoryService : IBlobInventoryService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobInventoryService> _logger;

    public BlobInventoryService(
        IOptions<AzureBlobOptions> options,
        ILogger<BlobInventoryService> logger)
    {
        var blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
        _logger = logger;
    }

    public async Task<List<BlobFileMetadata>> GetAllBlobsAsync()
    {
        var blobs = new List<BlobFileMetadata>();
        var supportedExtensions = new[] { ".pdf", ".csv", ".html", ".txt", ".docx", ".xlsx" };

        _logger.LogInformation("Starting blob inventory enumeration...");

        await foreach (var blobItem in _containerClient.GetBlobsAsync())
        {
            // Filter by supported document types
            var extension = Path.GetExtension(blobItem.Name).ToLowerInvariant();
            if (!supportedExtensions.Contains(extension))
            {
                _logger.LogDebug($"Skipping unsupported file type: {blobItem.Name}");
                continue;
            }

            var blobClient = _containerClient.GetBlobClient(blobItem.Name);
            var properties = blobItem.Properties;

            blobs.Add(new BlobFileMetadata
            {
                BlobPath = blobItem.Name,
                FileName = Path.GetFileName(blobItem.Name),
                SizeBytes = properties.ContentLength ?? 0,
                CreatedDate = properties.CreatedOn ?? DateTimeOffset.UtcNow,
                ModifiedDate = properties.LastModified ?? DateTimeOffset.UtcNow,
                ContentType = properties.ContentType ?? "application/octet-stream"
            });
        }

        _logger.LogInformation($"Inventory complete: {blobs.Count} eligible files discovered.");
        return blobs;
    }
}