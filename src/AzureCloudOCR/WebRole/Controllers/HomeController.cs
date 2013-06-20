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
using Recaptcha.Web;
using Recaptcha.Web.Mvc;
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
            NormalizeUploadImageViewModel(model);
            return View(model);
        }

        //
        // POST: /Home/UploadImage/

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("UploadImage")]
        public ActionResult UploadImage_Post(UploadImageViewModel model)
        {
            NormalizeUploadImageViewModel(model);
            
            var image = ValidateAndExtractImage(model.ImageFile);
            ValidateCaptcha();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var blob = UploadImageToStorage(image);
                var originalFileName = model.ImageFile.FileName;
                image.Dispose();

                CreateNewOCRTask(blob.Name, originalFileName, model.EmailAddress);
                return View("UploadImageSuccessful");
            }
            catch (Exception e)
            {
                Error("An error occurred: " + e.Message);
                return View(model);
            }
        }

        private void NormalizeUploadImageViewModel(UploadImageViewModel model)
        {
            if (model != null && model.CaptchaPublicKey == null)
            {
                model.CaptchaPublicKey = RecaptchaConfig.RecaptchaPublicKey;
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

        private void ValidateCaptcha()
        {
            RecaptchaVerificationHelper recaptchaHelper = this.GetRecaptchaVerificationHelper(RecaptchaConfig.RecaptchaPrivateKey);
            if (String.IsNullOrEmpty(recaptchaHelper.Response))
            {
                ModelState.AddModelError("captcha-answer-empty", "Verification code cannot be empty.");
                return;
            }

            RecaptchaVerificationResult recaptchaResult = recaptchaHelper.VerifyRecaptchaResponse();
            if (recaptchaResult != RecaptchaVerificationResult.Success)
            {
                ModelState.AddModelError("captcha-answer-incorrect", "Incorrect verification code.");
            }
        }

        private CloudBlockBlob UploadImageToStorage(Image image)
        {
            using (MemoryStream imageStream = new MemoryStream())
            {
                image.Save(imageStream, ImageFormat.Tiff);
                imageStream.Seek(0, SeekOrigin.Begin);

                string blobName = Guid.NewGuid() + ".tif";
                var blob = AzureBlobs.ImageBlobContainer.GetBlockBlobReference(blobName);
                blob.UploadFromStream(imageStream);
                return blob;
            }
        }

        private void CreateNewOCRTask(string blobName, string originalFileName, string emailAddress)
        {
            CreateJobLogEntry(blobName, originalFileName, emailAddress);
            CreateOCRQueueItem(blobName, emailAddress);
        }

        private static void CreateJobLogEntry(string blobName, string originalFileName, string emailAddress)
        {
            var jobRecord = new OCRJobRecord
            {
                DateTime = DateTime.UtcNow,
                FileName = originalFileName,
                ImageBlobName = blobName,
                EmailAddress = emailAddress,
            };
            AzureTables.AddOCRJobRecord(jobRecord);
        }

        private static void CreateOCRQueueItem(string blobName, string emailAddress)
        {
            var messageContent = string.Format("{0}|{1}", blobName, emailAddress);
            var queueMessage = new CloudQueueMessage(messageContent);
            AzureQueues.OCRQueue.AddMessage(queueMessage);
        }
    }
}
