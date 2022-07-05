// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


namespace Image.Commons.BoundingBox.Models
{
    /// <summary>
    /// Analyze Result class.
    /// </summary>
    public class AnalyzeResult
    {
        /// <summary>
        /// Gets or sets the read results.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the recognition results.
        /// </summary>
        /// <value>
        /// The recognition results.
        /// </value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Reviewed.")]
        public OcrPageLayoutV3[] ReadResults { get; set; }
    }
}
