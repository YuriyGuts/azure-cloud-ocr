using System;

namespace AzureStorageUtils.Entities
{
    public class OCRQueueMessage
    {
        public OCRQueueMessage(Guid jobID, string imageBlobName, string recipientEmail)
        {
            JobID = jobID;
            ImageBlobName = imageBlobName;
            RecipientEmail = recipientEmail;
        }

        public Guid JobID { get; set; }

        public string ImageBlobName { get; set; }

        public string RecipientEmail { get; set; }

        public static OCRQueueMessage Parse(string messageString)
        {
            var splitString = messageString.Split('|');
            var jobID = splitString[0];
            string imageBlobName = splitString[1];
            string recipientEmail = splitString[2];

            if (string.IsNullOrEmpty(jobID))
            {
                throw new FormatException("Job ID cannot be empty.");
            }
            if (string.IsNullOrEmpty(imageBlobName))
            {
                throw new FormatException("Image blob name cannot be empty.");
            }
            if (string.IsNullOrEmpty(recipientEmail))
            {
                throw new FormatException("Email address cannot be empty.");
            }

            var message = new OCRQueueMessage(Guid.Parse(jobID), imageBlobName, recipientEmail);
            return message;
        }

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}", JobID, ImageBlobName, RecipientEmail);
        }
    }
}
