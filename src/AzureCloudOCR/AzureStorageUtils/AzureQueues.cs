using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace AzureStorageUtils
{
    public static class AzureQueues
    {
        private static CloudStorageAccount storageAccount;
        private static CloudQueueClient queueClient;
        private static CloudQueue ocrQueue;
        private static CloudQueue emailQueue;

        public static CloudQueue OCRQueue
        {
            get { return ocrQueue; }
        }

        public static CloudQueue EmailQueue
        {
            get { return emailQueue; }
        }

        public static void Initialize(string storageConnectionString, string ocrQueueName, string emailQueueName)
        {
            storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            queueClient = storageAccount.CreateCloudQueueClient();

            ocrQueue = InitializeQueue(ocrQueueName);
            emailQueue = InitializeQueue(emailQueueName);
        }

        private static CloudQueue InitializeQueue(string queueName)
        {
            CloudQueue queue = null;
            if (queueName != null)
            {
                queue = queueClient.GetQueueReference(queueName);
                queue.CreateIfNotExists();
            }
            return queue;
        }
    }
}