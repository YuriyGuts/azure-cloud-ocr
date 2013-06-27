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
