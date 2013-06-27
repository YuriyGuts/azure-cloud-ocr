using System.ComponentModel.DataAnnotations;
using System.Web;
using Microsoft.Web.Mvc;

namespace WebRole.Models
{
    public class UploadImageViewModel
    {
        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "Please upload a valid image file.")]
        [Display(Name = "Image file")]
        public HttpPostedFileBase ImageFile { get; set; }
    }
}