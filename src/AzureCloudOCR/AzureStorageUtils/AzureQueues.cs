// Copyright (c) Yuriy Guts, 2013
// 
// Licensed under the Apache License, version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at:
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 
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