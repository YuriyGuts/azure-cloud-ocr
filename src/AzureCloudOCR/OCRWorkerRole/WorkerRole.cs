using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using AzureStorageUtils;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace OCRWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private bool onStopCalled;
        private bool returnedFromRunMethod;
        private const int maxSingleMessageDequeueCount = 10;

        public override void Run()
        {
            Trace.TraceInformation("OCRWorkerRole entry point called.", "Information");

            while (true)
            {
                Trace.TraceInformation("OCRWorkerRole is awake.", "Information");
                Trace.TraceInformation("OCR Queue has approximately {0} message(s).", AzureQueues.OCRQueue.ApproximateMessageCount);

                if (onStopCalled)
                {
                    Trace.TraceInformation("OnStop request caught in Run method.");
                    returnedFromRunMethod = true;
                    break;
                }

                var ocrMessageVisibilityTimeout = TimeSpan.FromMinutes(3);
                var ocrQueueRequestOptions = new QueueRequestOptions
                {
                    MaximumExecutionTime = TimeSpan.FromMinutes(15),
                    RetryPolicy = new LinearRetry(TimeSpan.FromMinutes(1), 5)
                };

                while (true)
                {
                    var queueMessage = AzureQueues.OCRQueue.GetMessage(ocrMessageVisibilityTimeout, ocrQueueRequestOptions);
                    if (queueMessage == null)
                    {
                        break;
                    }

                    ProcessOCRQueueMessage(queueMessage);
                }

                Trace.TraceInformation("No new messages to process yet. Sleeping.", "Information");
                Thread.Sleep(10000);
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            InitializeAzureStorage();

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }

        public override void OnStop()
        {
            Trace.TraceInformation("OnStop request received. Trying to stop...");
            
            onStopCalled = true;
            while (!returnedFromRunMethod)
            {
                Thread.Sleep(1000);
            }

            Trace.TraceInformation("OnStop request received. Trying to stop...");
        }

        private void InitializeAzureStorage()
        {
            Trace.TraceInformation("Initializing Azure Storage.");

            Trace.TraceInformation("Loading storage settings.");
            string storageConnectionString = RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString");
            string imageBlobContainerName = RoleEnvironment.GetConfigurationSettingValue("ImageBlobContainerName");
            string textBlobContainerName = RoleEnvironment.GetConfigurationSettingValue("TextBlobContainerName");
            string ocrQueueName = RoleEnvironment.GetConfigurationSettingValue("OCRQueueName");
            string emailQueueName = RoleEnvironment.GetConfigurationSettingValue("EmailQueueName");

            Trace.TraceInformation("Initializing Blob Storage.");
            AzureBlobs.Initialize(storageConnectionString, imageBlobContainerName, textBlobContainerName);

            Trace.TraceInformation("Initializing Queues.");
            AzureQueues.Initialize(storageConnectionString, ocrQueueName, emailQueueName);
        }

        private void ProcessOCRQueueMessage(CloudQueueMessage queueMessage)
        {
            string messageContent = queueMessage.AsString;
            Trace.TraceInformation("Processing OCR queue message: " + messageContent);

            // To protect from accidental poison messages that get stuck in the queue.
            if (queueMessage.DequeueCount > maxSingleMessageDequeueCount)
            {
                Trace.TraceInformation("Message max dequeue limit reached. Deleting it as a poison message.");
                AzureQueues.OCRQueue.DeleteMessage(queueMessage);
                return;
            }

            string blobName;
            string recipientEmail;

            try
            {
                var splitMessage = messageContent.Split('|');
                blobName = splitMessage[0];
                recipientEmail = splitMessage[1];

                if (string.IsNullOrEmpty(blobName) || string.IsNullOrEmpty(recipientEmail))
                {
                    throw new FormatException("Blob name and recipient email but be non-empty.");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Invalid OCR message format. Deleting. Details: " + ex.Message);
                AzureQueues.OCRQueue.DeleteMessage(queueMessage);
                return;
            }

            try
            {
                Image image = ReadImageFromBlob(blobName);
                string ocrText = RecognizeTextOnImage(image);
                CloudBlockBlob textBlob = StoreRecognizedTextAsBlob(ocrText, blobName + ".txt");
                CreateEmailTask(textBlob.Name, recipientEmail);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("An error occurred while processing OCR message. Details: " + ex.Message);
                return;
            }

            Trace.TraceInformation("OCR message successfully processed, deleting." + messageContent);
            AzureQueues.OCRQueue.DeleteMessage(queueMessage);
        }

        private Image ReadImageFromBlob(string blobName)
        {
            var blob = AzureBlobs.ImageBlobContainer.GetBlockBlobReference(blobName);
            var result = Image.FromStream(blob.OpenRead());
            return result;
        }

        private string RecognizeTextOnImage(Image image)
        {
            // TODO: Process image with OCR engine.
            return "This is the recognized text.";
        }

        private CloudBlockBlob StoreRecognizedTextAsBlob(string ocrText, string blobName)
        {
            var blob = AzureBlobs.TextBlobContainer.GetBlockBlobReference(blobName);
            using (MemoryStream textStream = new MemoryStream(Encoding.UTF8.GetBytes(ocrText)))
            {
                blob.UploadFromStream(textStream);
            }
            return blob;
        }

        private void CreateEmailTask(string textBlobName, string recipientEmail)
        {
            var messageContent = string.Format("{0}|{1}", textBlobName, recipientEmail);
            var queueMessage = new CloudQueueMessage(messageContent);
            AzureQueues.EmailQueue.AddMessage(queueMessage);
        }
    }
}
