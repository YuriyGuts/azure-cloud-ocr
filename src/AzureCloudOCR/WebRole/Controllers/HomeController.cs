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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using System.Web.Mvc;
using AzureStorageUtils;
using AzureStorageUtils.Entities;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Recaptcha;
using WebRole.Models;

namespace WebRole.Controllers
{
    public class HomeController : BootstrapBaseController
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        //
        // GET: /Home/UploadImage/

        [HttpGet]
        [ActionName("UploadImage")]
        public ActionResult UploadImage_Get(UploadImageViewModel model)
        {
            return View(model);
        }

        //
        // POST: /Home/UploadImage/

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RecaptchaControlMvc.CaptchaValidator]
        [ActionName("UploadImage")]
        public ActionResult UploadImage_Post(UploadImageViewModel model, bool captchaValid)
        {
            ValidateCaptcha(captchaValid);
            using (var image = ValidateAndExtractImage(model.ImageFile))
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                try
                {
                    var jobID = Guid.NewGuid();
                    var blob = UploadImageToStorage(image, jobID.ToString());
                    var originalFileName = model.ImageFile.FileName;
                    
                    CreateNewOCRTask(jobID, originalFileName, model.EmailAddress, blob.Name);
                    return View("UploadImageSuccessful");
                }
                catch (Exception e)
                {
                    Error("An error occurred: " + e.Message);
                    return View(model);
                }
            }
        }

        private Image ValidateAndExtractImage(HttpPostedFileBase imageFile)
        {
            Image result = null;
            try
            {
                result = Image.FromStream(imageFile.InputStream);
            }
            catch (Exception)
            {
                ModelState.AddModelError("invalid-image", "Invalid image or unsupported image format.");
            }
            return result;
        }

        private void ValidateCaptcha(bool captchaValid)
        {
            if (!captchaValid)
            {
                ModelState.AddModelError("captcha-incorrect", string.Format("Incorrect verification code."));
            }
        }

        private CloudBlockBlob UploadImageToStorage(Image image, string preferredName)
        {
            using (MemoryStream imageStream = new MemoryStream())
            {
                image.Save(imageStream, ImageFormat.Tiff);
                imageStream.Seek(0, SeekOrigin.Begin);

                string blobName = preferredName + ".tif";
                var blob = AzureBlobs.ImageBlobContainer.GetBlockBlobReference(blobName);
                blob.UploadFromStream(imageStream);
                return blob;
            }
        }

        private void CreateNewOCRTask(Guid jobID, string originalFileName, string emailAddress, string blobName)
        {
            CreateJobLogEntry(jobID, originalFileName, emailAddress);
            CreateOCRQueueItem(jobID, blobName, emailAddress);
        }

        private static void CreateJobLogEntry(Guid jobID, string originalFileName, string emailAddress)
        {
            var jobRecord = new OCRJob
            {
                ID = jobID,
                DateTime = DateTime.UtcNow,
                OriginalFileName = originalFileName,
                EmailAddress = emailAddress,
            };
            AzureTables.OCRJobRepository.AddOCRJob(jobRecord);
        }

        private static void CreateOCRQueueItem(Guid jobID, string blobName, string emailAddress)
        {
            var message = new OCRQueueMessage(jobID, blobName, emailAddress);
            var wrappedMessage = new CloudQueueMessage(message.ToString());
            AzureQueues.OCRQueue.AddMessage(wrappedMessage);
        }
    }
}
