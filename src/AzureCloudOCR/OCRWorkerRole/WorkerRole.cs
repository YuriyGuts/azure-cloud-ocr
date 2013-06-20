using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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
                Trace.TraceInformation("OCR Queue has approximately {0} message(s).", AzureQueues.OCRQueue.ApproximateMessageCount ?? 0);

                var ocrMessageVisibilityTimeout = TimeSpan.FromMinutes(1);
                var ocrQueueRequestOptions = new QueueRequestOptions
                {
                    MaximumExecutionTime = TimeSpan.FromMinutes(15),
                    RetryPolicy = new LinearRetry(TimeSpan.FromMinutes(1), 5)
                };

                while (true)
                {
                    if (onStopCalled)
                    {
                        Trace.TraceInformation("OnStop request caught in Run method. Stopping all work.");
                        returnedFromRunMethod = true;
                        return;
                    }

                    var queueMessage = AzureQueues.OCRQueue.GetMessage(ocrMessageVisibilityTimeout, ocrQueueRequestOptions);
                    if (queueMessage == null)
                    {
                        break;
                    }

                    ProcessOCRQueueMessage(queueMessage);
                }

                Trace.TraceInformation("No new messages to process. Sleeping.", "Information");
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

            string imageBlobName;
            string recipientEmail;

            try
            {
                var splitMessage = messageContent.Split('|');
                imageBlobName = splitMessage[0];
                recipientEmail = splitMessage[1];

                if (string.IsNullOrEmpty(imageBlobName) || string.IsNullOrEmpty(recipientEmail))
                {
                    throw new FormatException("Blob name and recipient email must be non-empty.");
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
                string imageFileName = SaveImageBlobToLocalFile(imageBlobName);
                string ocrTextFileName = PerformOCROnImageFile(imageFileName);
                string ocrBlobName = new FileInfo(ocrTextFileName).Name;
                CloudBlockBlob textBlob = StoreRecognizedTextAsBlob(ocrTextFileName, ocrBlobName);
                CreateEmailTask(textBlob.Name, recipientEmail);
                DeleteInputImage(imageBlobName);
                DeleteTemporaryFiles(imageFileName, ocrTextFileName);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("An error occurred while processing OCR message. Details: " + ex.Message);
                return;
            }

            Trace.TraceInformation("OCR message successfully processed, deleting." + messageContent);
            AzureQueues.OCRQueue.DeleteMessage(queueMessage);
        }

        private string SaveImageBlobToLocalFile(string imageBlobName)
        {
            var imageBlob = AzureBlobs.ImageBlobContainer.GetBlockBlobReference(imageBlobName);
            var localFileName = imageBlobName;
            using (FileStream outputFileStream = new FileStream(localFileName, FileMode.Create))
            {
                imageBlob.DownloadToStream(outputFileStream);
            }
            return localFileName;
        }

        private string PerformOCROnImageFile(string imageFileName)
        {
            // Tesseract uses a few 32-bit unmanaged libraries which cannot be loaded from a 64-bit managed assembly.
            // Windows Azure does not support x86 assemblies as worker roles; however, we can still launch an x86 .exe.
            // So, as a workaround, we'll delegate the work to an external process, TesseractProcessor.exe.

            string ocrOutputFileName = imageFileName + ".ocr.txt";
            string recognizerArguments = string.Format("\"{0}\" \"{1}\"", imageFileName, ocrOutputFileName);
            var recognizerStartInfo = new ProcessStartInfo
            {
                FileName = "TesseractProcessor.exe",
                Arguments = recognizerArguments,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            var recognizerProcess = Process.Start(recognizerStartInfo);
            
            // We can actually process multiple images in parallel processes but let's keep it simple for now.
            recognizerProcess.WaitForExit((int)TimeSpan.FromMinutes(2).TotalMilliseconds);
            
            return ocrOutputFileName;
        }

        private CloudBlockBlob StoreRecognizedTextAsBlob(string ocrTextFileName, string targetBlobName)
        {
            var blob = AzureBlobs.TextBlobContainer.GetBlockBlobReference(targetBlobName);
            using (FileStream ocrTextStream = new FileStream(ocrTextFileName, FileMode.Open))
            {
                blob.UploadFromStream(ocrTextStream);
            }
            return blob;
        }

        private void CreateEmailTask(string textBlobName, string recipientEmail)
        {
            var messageContent = string.Format("{0}|{1}", textBlobName, recipientEmail);
            var queueMessage = new CloudQueueMessage(messageContent);
            AzureQueues.EmailQueue.AddMessage(queueMessage);
        }

        private void DeleteInputImage(string imageBlobName)
        {
            var imageBlob = AzureBlobs.ImageBlobContainer.GetBlockBlobReference(imageBlobName);
            imageBlob.Delete();
        }

        private void DeleteTemporaryFiles(string imageFileName, string ocrTextFileName)
        {
            // A bit dirty, but to avoid issues with not-yet-released locks on temporary files.
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    File.Delete(imageFileName);
                    File.Delete(ocrTextFileName);
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
