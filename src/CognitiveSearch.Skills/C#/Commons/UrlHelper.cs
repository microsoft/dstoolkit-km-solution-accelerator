// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Web;

namespace Commons
{
    public class UrlUtility
    {
        public static string UrlDecode (string url)
        {
            return HttpUtility.UrlDecode(url.Replace("+", "%2B").Replace("(", "%28").Replace(")", "%29"));
        }

        public static string UrlEncode(string path)
        {
            return Uri.EscapeDataString(path);
        }
    }
}
