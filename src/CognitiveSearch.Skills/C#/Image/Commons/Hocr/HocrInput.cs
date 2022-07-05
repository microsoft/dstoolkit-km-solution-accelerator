// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Image.Commons.BoundingBox.Models;
using System.Collections.Generic;

namespace Image.Commons.Hocr
{
    public class HocrInput
    {
        public List<OcrPageLayoutV2> pages { get; set; }

        public HocrMetadata metadata { get; set; }
    }
}
