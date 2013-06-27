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
