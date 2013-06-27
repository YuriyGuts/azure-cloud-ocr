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
using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorageUtils.Entities
{
    public class OCRJobRepository
    {
        private readonly CloudTable table;

        public OCRJobRepository(CloudTable table)
        {
            this.table = table;
        }

        public OCRJob GetOCRJob(Guid id, string emailAddress)
        {
            TableOperation selectOperation = TableOperation.Retrieve<OCRJob>(emailAddress, id.ToString());
            return (OCRJob)table.Execute(selectOperation).Result;
        }

        public void AddOCRJob(OCRJob job)
        {
            job.PartitionKey = job.EmailAddress;
            job.RowKey = job.ID.ToString();

            TableOperation insertOperation = TableOperation.Insert(job);
            table.Execute(insertOperation);
        }

        public void UpdateOCRJob(OCRJob job)
        {
            TableOperation insertOperation = TableOperation.Replace(job);
            table.Execute(insertOperation);
        }
    }
}
