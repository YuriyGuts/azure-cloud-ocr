using Microsoft.WindowsAzure.ServiceRuntime;

namespace WebRole
{
    public static class RecaptchaConfig
    {
        private static string recaptchaPublicKey;
        private static string recaptchaPrivateKey;

        public static string RecaptchaPublicKey
        {
            get { return recaptchaPublicKey; }
        }

        public static string RecaptchaPrivateKey
        {
            get { return recaptchaPrivateKey; }
        }

        public static void InitializeRecaptcha()
        {
            recaptchaPublicKey = RoleEnvironment.GetConfigurationSettingValue("RecaptchaPublicKey");
            recaptchaPrivateKey = RoleEnvironment.GetConfigurationSettingValue("RecaptchaPrivateKey");
        }
    }
}