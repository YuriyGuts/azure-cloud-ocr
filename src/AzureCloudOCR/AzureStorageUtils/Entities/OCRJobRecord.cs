using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorageUtils.Entities
{
    public class OCRJobRecord : TableEntity
    {
        public string EmailAddress { get; set; }

        public string ImageBlobName { get; set; }

        public DateTime DateTime { get; set; }

        public string FileName { get; set; }       
    }
}
