using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PowerSchnell.Wrappers
{
    public class AzureBlobStorageWrapper : IAzureBlobStorageWrapper
    {
        private readonly BlobServiceClient _serviceClient;
        public AzureBlobStorageWrapper(string connectionString)
        {
            _serviceClient = new BlobServiceClient(connectionString);
        }

        public async Task<Stream?> RetrieveFromBlob(string containerName, string fileName)
        {
            Stream? result = null;
            BlobContainerClient blobContainerClient = _serviceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = blobContainerClient.GetBlobClient(fileName);
            if (await blobClient.ExistsAsync())
            {
                result = await blobClient.OpenReadAsync();
            }

            return result;
        }

        public async Task<BlobContentInfo> UploadBlob(string containerName, string fileName, Stream file)
        {
            BlobContainerClient blobContainerClient = _serviceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = blobContainerClient.GetBlobClient(fileName);
            return (await blobClient.UploadAsync(file)).Value;
        }

        public async Task<bool> DeleteBlob(string containerName, string fileName)
        {
            BlobContainerClient blobContainerClient = _serviceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = blobContainerClient.GetBlobClient(fileName);
            return (await blobClient.DeleteIfExistsAsync()).Value;
        }

        public async Task<bool> Exists(string containerName, string fileName) 
        {
            BlobContainerClient blobContainerClient = _serviceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = blobContainerClient.GetBlobClient(fileName);
            return (await blobClient.ExistsAsync()).Value;
        }
    }
}
