// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Image.Commons.BoundingBox.Models;
using System.IO;

namespace Image.Commons.Hocr
{
    public class HocrPage
    {
        readonly StringWriter metadata = new StringWriter();
        readonly StringWriter text = new StringWriter() { NewLine = " " };

        public HocrPage(HocrMetadata imageMetadata, OcrPageLayoutV2 page, int pageNumber)
        {
            pageNumber++;

            // page
            metadata.WriteLine($"<div class='ocr_page' id='page_{pageNumber}' title='image \"{imageMetadata.ImageStoreUri}\"; bbox 0 0 {imageMetadata.Width} {imageMetadata.Height}; ppageno {pageNumber}'>");
            metadata.WriteLine($"<div class='ocr_carea' id='block_{pageNumber}_1'>");

            int line = 0;

            foreach (NormalizedLine normline in page.Lines)
            {
                //metadata.WriteLine($"<span class='ocr_line' id='line_{pageNumber}_{line}' title='baseline -0.002 -5; x_size 30; x_descenders 6; x_ascenders 6'>");
                string bbox = normline.BoundingBox != null && normline.BoundingBox.Count == 4 ? $"bbox {normline.BoundingBox[0].X} {normline.BoundingBox[0].Y} {normline.BoundingBox[2].X} {normline.BoundingBox[2].Y}" : "";
                metadata.WriteLine($"<span class='ocr_line' id='line_{pageNumber}_{line}' title='{bbox}'>");

                text.WriteLine(normline.Text.Trim());

                line++;
                metadata.WriteLine("</span>"); // Line
            }
            metadata.WriteLine("</div>"); // Reading area
            metadata.WriteLine("</div>"); // Page
        }

        public string Metadata => metadata.ToString();
        public string Text => text.ToString();

    }
}
