using System.Web.Configuration;
using System.Web.Mvc;
using WebRole.Models;

namespace WebRole.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        //
        // GET: /Home/UploadImage/

        public ActionResult UploadImage()
        {
            return View(new UploadImageViewModel(WebConfigurationManager.AppSettings["recaptchaPublicKey"]));
        }

        //
        // POST: /Home/UploadImage/

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadImage(UploadImageViewModel model)
        {
            return Content("TODO: Process posted data.");
        }
    }
}
