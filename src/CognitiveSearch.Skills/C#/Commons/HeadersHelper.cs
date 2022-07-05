// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Commons
{
    public class HeadersHelper
    {
        public static CustomHeaders ConvertFunctionHeaders(IHeaderDictionary headers)
        {
            CustomHeaders myheaders = new CustomHeaders();

            IEnumerator<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> items = headers.GetEnumerator();

            foreach (var item in headers.Keys)
            {
                Microsoft.Extensions.Primitives.StringValues values = headers[item];

                myheaders.Add(item, String.Join(",",values.ToArray()));
            }

            return myheaders;
        }
    }

    public class CustomHeaders : Dictionary<string,string>
    {

    }
}
