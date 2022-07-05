// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Services.Common
{
    public class ResponseObject
    {
        public string status { get; set; }
        public string warnings{ get; set; }
        public bool HasEmbeddedObject { get; set; }
        public List<string> list { get; set; }
    }
}
