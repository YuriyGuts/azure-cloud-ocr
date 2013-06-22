using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using AzureStorageUtils;
using AzureStorageUtils.Entities;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace OCRWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private volatile bool onStopCalled;
        private volatile bool returnedFromRunMethod;

        private readonly TimeSpan ocrMessageVisibilityTimeout;
        private readonly QueueRequestOptions ocrQueueRequestOptions;
        private const int maxSingleMessageDequeueCount = 10;

        public WorkerRole()
        {
            ocrMessageVisibilityTimeout = TimeSpan.FromMinutes(1);
            ocrQueueRequestOptions = new QueueRequestOptions
            {
                MaximumExecutionTime = TimeSpan.FromMinutes(15),
                RetryPolicy = new LinearRetry(TimeSpan.FromMinutes(1), 5)
            };
        }

        public override void Run()
        {
            Trace.TraceInformation("OCRWorkerRole entry point called.", "Information");

            while (true)
            {
                Trace.TraceInformation("OCRWorkerRole is awake.", "Information");
                AzureQueues.OCRQueue.FetchAttributes();
                Trace.TraceInformation("OCR Queue has approximately {0} message(s).", AzureQueues.OCRQueue.ApproximateMessageCount ?? 0);

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

                    // To protect from accidental poison messages that get stuck in the queue.
                    if (queueMessage.DequeueCount > maxSingleMessageDequeueCount)
                    {
                        Trace.TraceInformation("Message max dequeue limit reached. Deleting it as a poison message.");
                        AzureQueues.OCRQueue.DeleteMessage(queueMessage);
                        return;
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

            Trace.TraceInformation("Ready to stop.");
            base.OnStop();
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
            string ocrJobTableName = RoleEnvironment.GetConfigurationSettingValue("OCRJobTableName");

            Trace.TraceInformation("Initializing Blobs.");
            AzureBlobs.Initialize(storageConnectionString, imageBlobContainerName, textBlobContainerName);

            Trace.TraceInformation("Initializing Queues.");
            AzureQueues.Initialize(storageConnectionString, ocrQueueName, emailQueueName);

            Trace.TraceInformation("Initializing Tables.");
            AzureTables.Initialize(storageConnectionString, ocrJobTableName);
        }

        private void ProcessOCRQueueMessage(CloudQueueMessage queueMessage)
        {
            var messageContent = queueMessage.AsString;
            Trace.TraceInformation("Processing OCR queue message: " + messageContent);

            OCRQueueMessage ocrMessage = null;
            Exception exception = null;

            try
            {
                ocrMessage = OCRQueueMessage.Parse(messageContent);
                string imageFileName = SaveImageBlobToLocalFile(ocrMessage.ImageBlobName);
                string ocrTextFileName = PerformOCROnImageFile(imageFileName);
                string ocrBlobName = new FileInfo(ocrTextFileName).Name;
                CloudBlockBlob textBlob = StoreRecognizedTextAsBlob(ocrTextFileName, ocrBlobName);
                CreateEmailTask(ocrMessage.JobID, textBlob.Name, ocrMessage.RecipientEmail);
                DeleteInputImage(ocrMessage.ImageBlobName);
                DeleteTemporaryFiles(imageFileName, ocrTextFileName);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("An error occurred while processing OCR message. Details: " + ex);
                exception = ex;

                if (ocrMessage == null)
                {
                    Trace.TraceInformation("Invalid message format. Deleting.");
                    AzureQueues.OCRQueue.DeleteMessage(queueMessage);
                    return;
                }
            }

            try
            {
                var job = AzureTables.OCRJobRepository.GetOCRJob(ocrMessage.JobID, ocrMessage.RecipientEmail);
                if (exception == null)
                {
                    job.IsCompleted = true;
                    job.ErrorMessage = null;
                    AzureTables.OCRJobRepository.UpdateOCRJob(job);

                    Trace.TraceInformation("Message (JobID = {0}) successfully processed, deleting.", ocrMessage.JobID);
                    AzureQueues.OCRQueue.DeleteMessage(queueMessage);
                }
                else
                {
                    job.IsCompleted = true;
                    job.ErrorMessage = exception.ToString();
                    AzureTables.OCRJobRepository.UpdateOCRJob(job);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Failed to update job status: " + ex);
            }
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

        private void CreateEmailTask(Guid jobID, string textBlobName, string recipientEmail)
        {
            var message = new EmailQueueMessage(jobID, textBlobName, recipientEmail);
            var wrappedMessage = new CloudQueueMessage(message.ToString());
            AzureQueues.EmailQueue.AddMessage(wrappedMessage);
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
