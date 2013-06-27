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
