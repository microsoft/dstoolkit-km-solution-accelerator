// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Services.Common.WebApiSkills;
using System;
using System.Collections.Generic;
using System.Text;

namespace Commons
{
    public class DurableInputRecord
    {
        public CustomHeaders headers { get; set; }

        public WebApiRequestRecord record { get; set; }
    }

}
