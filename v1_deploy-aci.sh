#!/bin/bash

# Configuration
RESOURCE_GROUP="your-rg"
ACR_LOGIN_SERVER="yourregistry.azurecr.io"
IMAGE_NAME="sharepoint-migration"
IMAGE_TAG="latest"
CONTAINER_NAME="sharepoint-migration-run-$(date +%s)"

# Build and push Docker image
echo "Building Docker image..."
docker build -t $ACR_LOGIN_SERVER/$IMAGE_NAME:$IMAGE_TAG .

echo "Pushing to ACR..."
docker push $ACR_LOGIN_SERVER/$IMAGE_NAME:$IMAGE_TAG

# Deploy to ACI
echo "Deploying to Azure Container Instances..."
az container create \
  --resource-group $RESOURCE_GROUP \
  --name $CONTAINER_NAME \
  --image $ACR_LOGIN_SERVER/$IMAGE_NAME:$IMAGE_TAG \
  --cpu 2 \
  --memory 4 \
  --restart-policy Never \
  --environment-variables \
    AzureBlob__ConnectionString="YOUR_CONNECTION_STRING" \
    SharePoint__ClientSecret="YOUR_SECRET" \
  --registry-login-server $ACR_LOGIN_SERVER \
  --registry-username <acr-username> \
  --registry-password <acr-password> \
  --logs

echo "Container deployment initiated: $CONTAINER_NAME"