using System;

namespace AzureStorageUtils.Entities
{
    public class EmailQueueMessage
    {
        public EmailQueueMessage(Guid jobID, string textBlobName, string recipientEmail)
        {
            JobID = jobID;
            TextBlobName = textBlobName;
            RecipientEmail = recipientEmail;
        }

        public Guid JobID { get; set; }

        public string TextBlobName { get; set; }

        public string RecipientEmail { get; set; }

        public static EmailQueueMessage Parse(string messageString)
        {
            var splitString = messageString.Split('|');
            var jobID = splitString[0];
            var textBlobName = splitString[1];
            var recipientEmail = splitString[2];

            if (string.IsNullOrEmpty(jobID))
            {
                throw new FormatException("Job ID cannot be empty.");
            }
            if (string.IsNullOrEmpty(textBlobName))
            {
                throw new FormatException("Text blob name cannot be empty.");
            }
            if (string.IsNullOrEmpty(recipientEmail))
            {
                throw new FormatException("Recipient email cannot be empty.");
            }

            var message = new EmailQueueMessage(Guid.Parse(jobID), textBlobName, recipientEmail);
            return message;
        }

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}", JobID, TextBlobName, RecipientEmail);
        }
    }
}
