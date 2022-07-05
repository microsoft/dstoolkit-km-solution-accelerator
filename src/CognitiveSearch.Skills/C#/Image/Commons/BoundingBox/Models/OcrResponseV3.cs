// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Image.Commons.BoundingBox.Models
{
    using System;

    /// <summary>
    /// OCR Response.
    /// </summary>
    public class OcrResponseV3
    {
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The API status.
        /// </value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the created date time.
        /// </summary>
        /// <value>
        /// The Created date time value.
        /// </value>
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the created date time.
        /// </summary>
        /// <value>
        /// The Created date time value.
        /// </value>
        public DateTime LastUpdatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the Analyze result block.
        /// </summary>
        /// <value>
        /// The Analyze result block.
        /// </value>
        public AnalyzeResult AnalyzeResult { get; set; }
    }
}
