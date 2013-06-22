using AzureStorageUtils.Entities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorageUtils
{
    public static class AzureTables
    {
        private static CloudStorageAccount storageAccount;
        private static CloudTableClient tableClient;
        private static OCRJobRepository ocrJobRepository;

        public static OCRJobRepository OCRJobRepository
        {
            get { return ocrJobRepository; }
        }

        public static void Initialize(string storageConnectionString, string ocrJobTableName)
        {
            storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            tableClient = storageAccount.CreateCloudTableClient();
            ocrJobRepository = InitializeOCRJobRepository(ocrJobTableName);
        }

        private static OCRJobRepository InitializeOCRJobRepository(string tableName)
        {
            var table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();
            return new OCRJobRepository(table);
        }
    }
}
