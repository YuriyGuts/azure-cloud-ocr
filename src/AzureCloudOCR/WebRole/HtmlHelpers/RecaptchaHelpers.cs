using System.Web.Mvc;
using Recaptcha;

namespace WebRole.HtmlHelpers
{
    public static class RecaptchaHelpers
    {
        public static MvcHtmlString Recaptcha(this HtmlHelper htmlHelper, string id, string theme)
        {
            var html = RecaptchaControlMvc.GenerateCaptcha(htmlHelper, id, theme);
            return MvcHtmlString.Create(html);
        }
    }
}