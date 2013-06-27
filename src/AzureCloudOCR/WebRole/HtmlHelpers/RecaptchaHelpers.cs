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