using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharePointMigration.Configuration;

namespace SharePointMigration.Services;

public interface IBlobDownloadService
{
    Task<Stream> DownloadBlobAsync(string blobPath, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadBlobAsBytesAsync(string blobPath, CancellationToken cancellationToken = default);
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

    public async Task<Stream> DownloadBlobAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobPath);
            var download = await blobClient.DownloadAsync(cancellationToken);
            
            var memoryStream = new MemoryStream();
            await download.Value.Content.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            
            _logger.LogDebug($"Downloaded blob stream: {blobPath}");
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading blob: {blobPath}");
            throw;
        }
    }

    public async Task<byte[]> DownloadBlobAsBytesAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        using var stream = await DownloadBlobAsync(blobPath, cancellationToken);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}