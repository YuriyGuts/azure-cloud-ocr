using System.ComponentModel.DataAnnotations;
using System.Web;
using Microsoft.Web.Mvc;

namespace WebRole.Models
{
    public class UploadImageViewModel
    {
        public UploadImageViewModel() : this(null)
        {
        }

        public UploadImageViewModel(string captchaPublicKey)
        {
            CaptchaPublicKey = captchaPublicKey;
        }

        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "Please upload a valid image file.")]
        [Display(Name = "Image file")]
        public HttpPostedFileBase ImageFile { get; set; }

        public string CaptchaPublicKey { get; set; }
    }
}