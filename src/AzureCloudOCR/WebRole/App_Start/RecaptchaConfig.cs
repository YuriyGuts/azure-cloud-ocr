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
using Microsoft.WindowsAzure.ServiceRuntime;
using Recaptcha;

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

            RecaptchaControlMvc.PublicKey = recaptchaPublicKey;
            RecaptchaControlMvc.PrivateKey = recaptchaPrivateKey;
        }
    }
}