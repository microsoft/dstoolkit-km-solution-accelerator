// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Image.Commons.BoundingBox.Models
{
    /// <summary>
    /// OCR Layout Text.
    /// </summary>
    public class OcrPageLayoutV3 : AbstractPageLayout
    {
        /// <summary>
        /// Gets or sets the clockwise orientation.
        /// </summary>
        /// <value>
        /// The clockwise orientation.
        /// </value>
        public double Angle { get; set; }
    }
}
