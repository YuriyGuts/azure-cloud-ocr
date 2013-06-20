using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using AzureStorageUtils;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using SendGridMail;
using SendGridMail.Transport;

namespace EmailWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private bool onStopCalled;
        private bool returnedFromRunMethod;
        private Web sendGridTransport;
        private const int maxSingleMessageDequeueCount = 10;

        public override void Run()
        {
            Trace.TraceInformation("EmailWorkerRole entry point called.", "Information");

            while (true)
            {
                Trace.TraceInformation("EmailWorkerRole is awake.", "Information");
                Trace.TraceInformation("Email Queue has approximately {0} message(s).", AzureQueues.EmailQueue.ApproximateMessageCount ?? 0);

                var emailQueueMessageVisibilityTimeout = TimeSpan.FromMinutes(1);
                var emailQueueRequestOptions = new QueueRequestOptions
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

                    var queueMessage = AzureQueues.EmailQueue.GetMessage(emailQueueMessageVisibilityTimeout, emailQueueRequestOptions);
                    if (queueMessage == null)
                    {
                        break;
                    }

                    ProcessEmailQueueMessage(queueMessage);
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
            InitializeSendGridMailer();

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
            string textBlobContainerName = RoleEnvironment.GetConfigurationSettingValue("TextBlobContainerName");
            string emailQueueName = RoleEnvironment.GetConfigurationSettingValue("EmailQueueName");

            Trace.TraceInformation("Initializing Blob Storage.");
            AzureBlobs.Initialize(storageConnectionString, null, textBlobContainerName);

            Trace.TraceInformation("Initializing Queues.");
            AzureQueues.Initialize(storageConnectionString, null, emailQueueName);
        }

        private void InitializeSendGridMailer()
        {
            Trace.TraceInformation("Initializing SendGrid mailer.");

            Trace.TraceInformation("Loading settings.");
            string userName = RoleEnvironment.GetConfigurationSettingValue("SendGridUserName");
            string password = RoleEnvironment.GetConfigurationSettingValue("SendGridPassword");

            NetworkCredential sendGridCredentials = new NetworkCredential(userName, password);
            sendGridTransport = Web.GetInstance(sendGridCredentials);
        }

        private void ProcessEmailQueueMessage(CloudQueueMessage queueMessage)
        {
            string messageContent = queueMessage.AsString;
            Trace.TraceInformation("Processing email queue message: " + messageContent);

            // To protect from accidental poison messages that get stuck in the queue.
            if (queueMessage.DequeueCount > maxSingleMessageDequeueCount)
            {
                Trace.TraceInformation("Message max dequeue limit reached. Deleting it as a poison message.");
                AzureQueues.EmailQueue.DeleteMessage(queueMessage);
                return;
            }

            string textBlobName;
            string recipientEmail;

            try
            {
                var splitMessage = messageContent.Split('|');
                textBlobName = splitMessage[0];
                recipientEmail = splitMessage[1];

                if (string.IsNullOrEmpty(textBlobName) || string.IsNullOrEmpty(recipientEmail))
                {
                    throw new FormatException("Blob name and recipient email must be non-empty.");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Invalid queue message format. Deleting. Details: " + ex.Message);
                AzureQueues.EmailQueue.DeleteMessage(queueMessage);
                return;
            }

            try
            {
                SendOCRTextByEmail(textBlobName, recipientEmail);
                DeleteTextBlob(textBlobName);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("An error occurred while processing the message. Details: " + ex.Message);
                return;
            }

            Trace.TraceInformation("Email queue message successfully processed, deleting." + messageContent);
            AzureQueues.EmailQueue.DeleteMessage(queueMessage);
        }

        private void SendOCRTextByEmail(string textBlobName, string recipientEmail)
        {
            var from = new MailAddress("azure-cloud-ocr@eleks.com", "Windows Azure Cloud OCR");
            var to = new[] { new MailAddress(recipientEmail) };
            var subject = "Image recognition results";
            var text = "Please find your recognized text attached.";
            var html = text;

            using (MemoryStream attachmentStream = new MemoryStream())
            {
                var blob = AzureBlobs.TextBlobContainer.GetBlockBlobReference(textBlobName);
                blob.DownloadToStream(attachmentStream);
                attachmentStream.Seek(0, SeekOrigin.Begin);

                var sendGrid = SendGrid.GetInstance(from, to, null, null, subject, html, text);
                sendGrid.AddAttachment(attachmentStream, "recognized-text.txt");
                sendGridTransport.Deliver(sendGrid);
            }
        }

        private void DeleteTextBlob(string textBlobName)
        {
            var textBlob = AzureBlobs.TextBlobContainer.GetBlockBlobReference(textBlobName);
            textBlob.Delete();
        }
    }
}
