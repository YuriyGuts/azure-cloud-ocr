using System.ComponentModel.DataAnnotations;
using System.Web;

namespace WebRole.Models
{
    public class UploadImageViewModel
    {
        private readonly string captchaPublicKey;

        public UploadImageViewModel() : this(null)
        {
        }

        public UploadImageViewModel(string captchaPublicKey)
        {
            this.captchaPublicKey = captchaPublicKey;
        }

        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }

        [Display(Name = "Image file")]
        public HttpPostedFileBase ImageFile { get; set; }

        public string CaptchaPublicKey
        {
            get { return captchaPublicKey; }
        }

        [Display(Name = "Verification code")]
        public string CaptchaValue { get; set; }
    }
}