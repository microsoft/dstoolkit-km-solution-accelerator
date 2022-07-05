// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Image.Commons.BoundingBox.Models
{
    public class OcrPageLayoutV2 : AbstractPageLayout
    {
        /// <summary>
        /// Gets or sets the clockwise orientation.
        /// </summary>
        /// <value>
        /// The clockwise orientation.
        /// </value>
        public double ClockwiseOrientation { get; set; }
    }
}
