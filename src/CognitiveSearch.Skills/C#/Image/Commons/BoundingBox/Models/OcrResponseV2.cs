// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Image.Commons.BoundingBox.Models
{
    /// <summary>
    /// OCR Response.
    /// </summary>
    public class OcrResponseV2
    {
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The API status.
        /// </value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the recognition results.
        /// </summary>
        /// <value>
        /// The recognition results.
        /// </value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Reviewed.")]
        public OcrPageLayoutV2[] RecognitionResults { get; set; }
    }
}
