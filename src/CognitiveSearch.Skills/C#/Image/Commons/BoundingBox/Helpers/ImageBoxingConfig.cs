// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Image.Commons.BoundingBox.Helpers
{

    public class ImageBoxingConfig
    {
        public static readonly string OCR_DEFAULT_UNIT = "pixel";

        public Dictionary<string, BoxConversionUnit> config { get; set; }
    }

    public class BoxConversionUnit
    {
        public double ImageTextBoxingXThreshold { get; set; }
        public double ImageTextBoxingYThreshold { get; set; }
        public double ImageTextBoxingBulletListAdjustment { get; set; }

        public Dictionary<string, UnitThresholds> Thresholds { get; set; }
    }

    public class UnitThresholds
    {
        public double Xthresholdratio { get; set; }
        public double Ythresholdratio { get; set; }
    }
}
