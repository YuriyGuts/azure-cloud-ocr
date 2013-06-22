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
