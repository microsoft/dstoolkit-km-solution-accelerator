// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Image.Commons.BoundingBox.Models
{
    using System.Collections.Generic;
    using Image.Commons.BoundingBox.Helpers;
    using Newtonsoft.Json;

    /// <summary>
    /// Normalized line
    /// </summary>
    public class NormalizedLine
    {
        /// <summary>
        /// Gets or sets the bounding box.
        /// </summary>
        /// <value>
        /// The bounding box.
        /// </value>
        [JsonConverter(typeof(BoundingBoxConverter))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Reviewed.")]
        public List<Point> BoundingBox { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="NormalizedLine"/> is merged.
        /// </summary>
        /// <value>
        ///   <c>true</c> if merged; otherwise, <c>false</c>.
        /// </value>
        public bool Merged { get; set; }

        /// <summary>
        /// Gets the x median.
        /// </summary>
        /// <value>
        /// The x median.
        /// </value>
        public double XMedian
        {
            get => (BoundingBox[0].X + BoundingBox[1].X) / 2;
        }
    }
}
