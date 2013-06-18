using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorageUtils
{
    public static class AzureBlobs
    {
        private static CloudStorageAccount storageAccount;
        private static CloudBlobClient blobClient;
        private static CloudBlobContainer imageBlobContainer;
        private static CloudBlobContainer textBlobContainer;

        public static CloudBlobContainer ImageBlobContainer
        {
            get { return imageBlobContainer; }
        }

        public static CloudBlobContainer TextBlobContainer
        {
            get { return textBlobContainer; }
        }

        public static void Initialize(string storageConnectionString, string imageBlobContainerName, string textBlobContainerName)
        {
            storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            blobClient = storageAccount.CreateCloudBlobClient();

            imageBlobContainer = InitializeBlobContainer(imageBlobContainerName);
            textBlobContainer = InitializeBlobContainer(textBlobContainerName);
        }

        private static CloudBlobContainer InitializeBlobContainer(string blobContainerName)
        {
            CloudBlobContainer blobContainer = null;
            if (blobContainerName != null)
            {
                blobContainer = blobClient.GetContainerReference(blobContainerName);
                blobContainer.CreateIfNotExists();
            }
            return blobContainer;
        }
    }
}
