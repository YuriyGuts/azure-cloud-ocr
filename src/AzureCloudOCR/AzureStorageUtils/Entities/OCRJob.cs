using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorageUtils.Entities
{
    public class OCRJob : TableEntity
    {
        public Guid ID { get; set; }

        public string EmailAddress { get; set; }

        public string OriginalFileName { get; set; }

        public DateTime DateTime { get; set; }

        public bool IsCompleted { get; set; }

        public string ErrorMessage { get; set; }
    }
}