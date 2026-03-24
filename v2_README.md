# Azure Blob Storage → SharePoint Online ETL Pipeline

[![.NET Build & Test](https://github.com/Varun1585/azure-blob-to-sharepoint-etl/workflows/.NET%20Build%20&%20Test/badge.svg)](https://github.com/Varun1585/azure-blob-to-sharepoint-etl/actions)
[![Docker Build & Push](https://github.com/Varun1585/azure-blob-to-sharepoint-etl/workflows/Docker%20Build%20&%20Push/badge.svg)](https://github.com/Varun1585/azure-blob-to-sharepoint-etl/actions)

A robust, enterprise-grade .NET 8 ETL application for migrating documents from Azure Blob Storage to SharePoint Online with intelligent path transformation and comprehensive reporting.

## 🎯 Features

- ✅ **Secure Blob Enumeration** - List all files with metadata extraction
- ✅ **Flexible Path Transformation** - Regex-based mapping rules for folder reorganization
- ✅ **Batch Processing** - Configurable batch sizes with concurrent operations
- ✅ **Retry Logic** - Exponential backoff for transient failures
- ✅ **Comprehensive Reporting** - CSV, JSON, and detailed summary reports
- ✅ **Long-Running** - No time limits—deploy to Container Instances or App Service
- ✅ **SharePoint URLs** - Final URLs for all migrated files
- ✅ **Graceful Shutdown** - Supports Ctrl+C cancellation
- ✅ **Structured Logging** - Serilog with file and console outputs
- ✅ **Docker Ready** - Production-grade containerization

## 📋 Supported Document Types

PDF, CSV, HTML, TXT, DOCX, XLSX, PPTX, JSON, XML, LOG, RTF

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK or Docker
- Azure Storage Account with blob container
- SharePoint Online site with document library
- Azure AD app registration

### Local Development

```bash
# Clone repository
git clone https://github.com/Varun1585/azure-blob-to-sharepoint-etl.git
cd azure-blob-to-sharepoint-etl

# Configure
cp appsettings.json appsettings.local.json
# Edit appsettings.local.json with your credentials

# Run
dotnet run --configuration Development