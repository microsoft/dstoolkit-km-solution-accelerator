// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Helpers
{
    using System.Web;
    
    public class UrlUtility
    {
        public static string UrlDecode (string url)
        {
            return HttpUtility.UrlDecode(url.Replace("+", "%2B").Replace("(", "%28").Replace(")", "%29"));
        }
    }
}
