// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Services.Commons
{
    public class CustomStringComparer: IComparer<String>
    {
        public int Compare([AllowNull] string x, [AllowNull] string y)
        {
            return x.ToUpperInvariant().CompareTo(y.ToUpperInvariant());
        }
    }
}
