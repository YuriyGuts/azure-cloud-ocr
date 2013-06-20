using AzureStorageUtils.Entities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorageUtils
{
    public static class AzureTables
    {
        private static CloudStorageAccount storageAccount;
        private static CloudTableClient tableClient;
        private static CloudTable ocrJobTable;

        public static CloudTable OCRJobTable
        {
            get { return ocrJobTable; }
        }

        public static void Initialize(string storageConnectionString, string ocrJobTableName)
        {
            storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            tableClient = storageAccount.CreateCloudTableClient();
            ocrJobTable = InitializeTable(ocrJobTableName);
        }

        private static CloudTable InitializeTable(string tableName)
        {
            var table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();
            return table;
        }

        public static void AddOCRJobRecord(OCRJobRecord jobRecord)
        {
            jobRecord.PartitionKey = jobRecord.EmailAddress;
            jobRecord.RowKey = jobRecord.ImageBlobName;

            TableOperation insertOperation = TableOperation.Insert(jobRecord);
            ocrJobTable.Execute(insertOperation);
        }
    }
}
