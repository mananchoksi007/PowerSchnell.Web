using Azure.Storage.Blobs.Models;

namespace PowerSchnell.Wrappers
{
    public interface IAzureBlobStorageWrapper
    {
        public Task<Stream?> RetrieveFromBlob(string containerName, string fileName);
        public Task<BlobContentInfo> UploadBlob(string containerName, string fileName, Stream file);
        public Task<bool> DeleteBlob(string containerName, string fileName);
        public Task<bool> Exists(string containerName, string fileName);
    }
}
