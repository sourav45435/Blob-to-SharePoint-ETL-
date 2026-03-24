namespace SharePointMigration;

public class SharePointService
{
    private readonly string _siteUrl;
    private readonly string _libraryName;

    public SharePointService(string siteUrl, string libraryName)
    {
        _siteUrl = siteUrl;
        _libraryName = libraryName;
    }

    public async Task<SharePointUploadResult> UploadFileAsync(string fileName, byte[] fileContent)
    {
        Console.WriteLine($"   ⏳ Uploading: {fileName}...");

        try
        {
            // Simulate upload (replace with real SharePoint API call)
            await Task.Delay(500); // Simulate network delay

            var sharePointUrl = $"{_siteUrl}/{_libraryName}/{fileName}";

            return new SharePointUploadResult
            {
                Success = true,
                FileName = fileName,
                Url = sharePointUrl,
                UploadedAt = DateTime.Now,
                Message = "File uploaded successfully"
            };
        }
        catch (Exception ex)
        {
            return new SharePointUploadResult
            {
                Success = false,
                FileName = fileName,
                Message = $"Upload failed: {ex.Message}"
            };
        }
    }

    public SharePointUploadResult GenerateSharePointUrl(string fileName)
    {
        var url = $"{_siteUrl}/{_libraryName}/{fileName}";
        return new SharePointUploadResult
        {
            Success = true,
            FileName = fileName,
            Url = url
        };
    }
}

public class SharePointUploadResult
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.Now;
}