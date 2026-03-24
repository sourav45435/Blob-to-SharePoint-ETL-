# Azure Blob Storage → SharePoint Online ETL Pipeline

A robust, enterprise-grade .NET 8 ETL application for migrating documents from Azure Blob Storage to SharePoint Online with path transformation and comprehensive reporting.

## Features

- ✅ **Secure blob enumeration** with metadata extraction
- ✅ **Flexible path transformation** using mapping rules
- ✅ **Batch processing** with configurable concurrency
- ✅ **Retry logic** with exponential backoff
- ✅ **Comprehensive reporting** (CSV, JSON, detailed logs)
- ✅ **Long-running support** - deployable to Azure Container Instances or App Service
- ✅ **Graceful shutdown** - supports Ctrl+C cancellation
- ✅ **Structured logging** with Serilog

## Supported Document Types

- PDF, CSV, HTML, TXT, DOCX, XLSX, PPTX, JSON, XML, LOG, RTF

## Prerequisites

- .NET 8 SDK
- Azure Storage Account with blob container
- SharePoint Online site with document library
- Azure AD app registration (for authentication)

## Configuration

### 1. Update `appsettings.json`

```json
{
  "AzureBlob": {
    "ConnectionString": "Your connection string here",
    "ContainerName": "Your container name"
  },
  "SharePoint": {
    "SiteUrl": "https://yourtenant.sharepoint.com/sites/yoursite",
    "LibraryName": "Documents",
    "ClientId": "Your app ID",
    "ClientSecret": "Your app secret",
    "TenantId": "Your tenant ID"
  }
}
```

### 2. Define Path Mappings in `pathMappings.json`

```json
{
  "mappings": [
    {
      "sourcePrefix": "raw-documents/",
      "targetPrefix": "Documents/Processed/",
      "enabled": true
    }
  ]
}
```

## Running Locally

```bash
# Restore dependencies
dotnet restore

# Run
dotnet run

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Run with custom configuration
dotnet run --configuration Release
```

## Docker Deployment

```bash
# Build
docker build -t sharepoint-etl:latest .

# Run locally
docker run --rm \
  -e SP_AZUREBLOB__CONNECTIONSTRING="Your connection string" \
  -e SP_AZUREBLOB__CONTAINERNAME="documents" \
  -v $(pwd)/reports:/app/reports \
  sharepoint-etl:latest

# Push to Azure Container Registry
az acr build --registry yourregistry --image sharepoint-etl:latest .

# Deploy to Azure Container Instances
az container create \
  --resource-group your-rg \
  --name sharepoint-migration \
  --image yourregistry.azurecr.io/sharepoint-etl:latest \
  --cpu 2 \
  --memory 4 \
  --environment-variables \
    SP_AZUREBLOB__CONNECTIONSTRING="Your connection string" \
    SP_AZUREBLOB__CONTAINERNAME="documents"
```

## Output Reports

The application generates reports in the `reports/` directory:

- `migration_details_[timestamp].csv` - Detailed file-by-file report
- `migration_summary_[timestamp].json` - JSON summary with statistics
- `migration_summary_[timestamp].txt` - Human-readable summary
- `migration_detailed_[timestamp].txt` - Detailed migration log

## Logging

Logs are written to:
- **Console** - Real-time progress updates
- **File** - `logs/migration-[date].txt` (rolling daily files, 30-day retention)

## Architecture

```
┌─ BlobInventoryService
│  └─ Enumerate blobs, extract metadata
├─ PathTransformationService
│  └─ Apply mapping rules to paths
├─ BlobDownloadService
│  └─ Download blob content
├─ SharePointMigrationService
│  └─ Execute migration with retry logic
├─ ReportingService
│  └─ Generate completion reports
└─ ETLOrchestrator
   └─ Coordinate pipeline execution
```

## Error Handling

- Automatic retry with exponential backoff
- Failed files tracked with error reasons
- Detailed error logging and reporting
- Graceful degradation (partial failures don't stop pipeline)

## Performance Tuning

Adjust in `appsettings.json`:

- `AzureBlob.MaxDegreeOfParallelism` - Concurrent blob operations
- `SharePoint.BatchSize` - Files per batch
- `SharePoint.MaxRetries` - Retry attempts
- `Migration.ConcurrencyLevel` - Concurrent upload threads

## Troubleshooting

### No files migrated
- Check blob container name in config
- Verify file types are in supported list
- Check path mappings for syntax errors

### Authentication failures
- Verify Azure AD app registration credentials
- Ensure app has required permissions to SharePoint
- Check token expiration

### Out of memory
- Reduce `ConcurrencyLevel` in config
- Reduce `BatchSize`
- Increase container memory allocation

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit changes
4. Push to branch
5. Create Pull Request

## License

MIT License - See LICENSE file for details

## Support

For issues or questions, please create a GitHub issue or contact the development team.