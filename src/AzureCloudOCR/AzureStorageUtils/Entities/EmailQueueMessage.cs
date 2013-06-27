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
