using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharePointMigration.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SharePointMigration.Services;

public interface IBlobDownloadService
{
    Task<Stream> DownloadBlobAsync(string blobPath);
}

public class BlobDownloadService : IBlobDownloadService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobDownloadService> _logger;

    public BlobDownloadService(
        IOptions<AzureBlobOptions> options,
        ILogger<BlobDownloadService> logger)
    {
        var blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
        _logger = logger;
    }

    public async Task<Stream> DownloadBlobAsync(string blobPath)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobPath);
            var download = await blobClient.DownloadAsync();
            
            var memoryStream = new MemoryStream();
            await download.Value.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            _logger.LogDebug($"Downloaded blob: {blobPath}");
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading blob: {blobPath}");
            throw;
        }
    }
}