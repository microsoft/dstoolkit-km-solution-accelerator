// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Image.Commons.BoundingBox.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Image.Commons.BoundingBox.Helpers
{
    public class ImageBoxingHelper
    {
        // OCR Result Processing 

        /// <summary>
        /// The left alignment
        /// </summary>
        public const string LeftAlignment = "LeftAlignment";

        /// <summary>
        /// The right alignment
        /// </summary>
        public const string RightAlignment = "RightAlignment";

        /// <summary>
        /// The centered alignment
        /// </summary>
        public const string CenteredAlignment = "CenteredAlignment";

        /// <summary>
        /// The image text boxing x threshold.
        /// </summary>
        //private static readonly int ImageTextBoxingXThreshold = SPOCPI.Common.ConfigHelper.IntegerReader(Constants.ImageTextBoxingXThreshold, 6);
        private static readonly int ImageTextBoxingXThreshold = 6;

        /// <summary>
        /// The image text boxing y threshold.
        /// </summary>
        //private static readonly int ImageTextBoxingYThreshold = SPOCPI.Common.ConfigHelper.IntegerReader(Constants.ImageTextBoxingYThreshold, 10);
        private static readonly int ImageTextBoxingYThreshold = 10;

        /// <summary>
        /// The image text boxing bullet list adjustment.
        /// </summary>
        //private static readonly int ImageTextBoxingBulletListAdjustment = SPOCPI.Common.ConfigHelper.IntegerReader(Constants.ImageTextBoxingBulletListAdjustment, 20);
        private static readonly int ImageTextBoxingBulletListAdjustment = 20;


        //public static readonly ImageBoxingConfig ocrconfig = null ;

        //static ImageBoxingHelper()
        //{
        //    var assembly = Assembly.GetExecutingAssembly();
        //    var resourceName = "ImageSearch.Commons.BoundingBox.config.json";

        //    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        //    using (StreamReader reader = new StreamReader(stream))
        //    {
        //        ocrconfig = JsonConvert.DeserializeObject<ImageBoxingConfig>(reader.ReadToEnd());            
        //    }
        //}
        public static string ProcessOcrResponse(OcrResponseV3 response)
        {
            string newtext = string.Empty;

            foreach (OcrPageLayoutV3 item in response.AnalyzeResult.ReadResults)
            {
                double rotation = (double)Math.Round(item.Angle, 0);

                if (rotation >= 360 - 1 || rotation == 0)
                {
                    // do Nothing
                }
                else if (rotation >= 270 - 1)
                {
                    //Rotate 90 clockwise
                    item.Lines.ForEach(x =>
                    {
                        x.BoundingBox = RotateBoundingBox(item.Width, item.Height, x.BoundingBox, -90);
                    });
                }
                else if (rotation >= 180 - 1)
                {
                    // Rotate 180
                    item.Lines.ForEach(x =>
                    {
                        x.BoundingBox = RotateBoundingBox(item.Width, item.Height, x.BoundingBox, 180);
                    });
                }
                else if (rotation >= 90 - 1)
                {
                    //Rotate 90 counterclockwise
                    item.Lines.ForEach(x =>
                    {
                        x.BoundingBox = RotateBoundingBox(item.Width, item.Height, x.BoundingBox, 90);
                    });
                }
                else
                {
                    // Do Nothing for now . Will deal with that later
                }

                newtext += ProcessOCRPageLayout(item).Text;
                newtext += Environment.NewLine;
            }

            return newtext.ToString();
        }
        public static string ProcessOcrResponse(OcrResponseV2 response)
        {
            string newtext = string.Empty;

            foreach (OcrPageLayoutV2 item in response.RecognitionResults)
            {
                double rotation = (double)Math.Round(item.ClockwiseOrientation, 0);

                if (rotation >= 360 - 1 || rotation == 0)
                {
                    // do Nothing
                }
                else if (rotation >= 270 - 1)
                {
                    //Rotate 90 clockwise
                    item.Lines.ForEach(x =>
                    {
                        x.BoundingBox = RotateBoundingBox(item.Width, item.Height, x.BoundingBox, -90);
                    });
                }
                else if (rotation >= 180 - 1)
                {
                    // Rotate 180
                    item.Lines.ForEach(x =>
                    {
                        x.BoundingBox = RotateBoundingBox(item.Width, item.Height, x.BoundingBox, 180);
                    });
                }
                else if (rotation >= 90 - 1)
                {
                    //Rotate 90 counterclockwise
                    item.Lines.ForEach(x =>
                    {
                        x.BoundingBox = RotateBoundingBox(item.Width, item.Height, x.BoundingBox, 90);
                    });
                }
                else
                {
                    // Do Nothing for now . Will deal with that later
                }

                newtext += ProcessOCRPageLayout(item).Text;
                newtext += Environment.NewLine;
            }

            return newtext.ToString();
        }

        private static List<Point> RotateBoundingBox(double Width, double Height, List<Point> boundingBox, int rotationv)
        {
            List<Point> newboundary = new List<Point>();

            if (rotationv == 90)
            {
                newboundary.Add(boundingBox[0].inv());
                newboundary.Add(boundingBox[1].inv());
                newboundary.Add(boundingBox[2].inv());
                newboundary.Add(boundingBox[3].inv());

                //Adjusting the Y axis
                newboundary[0].Y = Width - boundingBox[0].X;
                newboundary[1].Y = Width - boundingBox[1].X;
                newboundary[2].Y = Width - boundingBox[2].X;
                newboundary[3].Y = Width - boundingBox[3].X;

            }
            else if (rotationv == -90)
            {
                newboundary.Add(boundingBox[0].inv());
                newboundary.Add(boundingBox[1].inv());
                newboundary.Add(boundingBox[2].inv());
                newboundary.Add(boundingBox[3].inv());

                //Adjusting the X axis 
                newboundary[0].X = Height - boundingBox[1].Y;
                newboundary[1].X = Height - boundingBox[0].Y;
                newboundary[2].X = Height - boundingBox[3].Y;
                newboundary[3].X = Height - boundingBox[2].Y;
            }
            else if (rotationv == 180)
            {
                newboundary.Add(boundingBox[1]);
                newboundary.Add(boundingBox[0]);
                newboundary.Add(boundingBox[3]);
                newboundary.Add(boundingBox[2]);

                //Adjust the Y axis 
                newboundary[0].Y = Height - boundingBox[1].Y;
                newboundary[1].Y = Height - boundingBox[0].Y;
                newboundary[2].Y = Height - boundingBox[3].Y;
                newboundary[3].Y = Height - boundingBox[2].Y;
            }
            else
            {
                newboundary.AddRange(boundingBox);
            }

            return newboundary;
        }

        public static AbstractPageLayout ProcessOCRPageLayout(AbstractPageLayout layout)
        {
            // Left-Aligned Text
            processLineBoundingBoxes(layout, LeftAlignment);
            // Right-Aligned Text
            processLineBoundingBoxes(layout, RightAlignment);
            // Center-Aligned Text
            processLineBoundingBoxes(layout, CenteredAlignment);

            List<NormalizedLine> XSortedList = null;

            XSortedList = layout.Lines.Where(o => o.Merged == false).OrderBy(o => o.BoundingBox[0].Y * o.BoundingBox[0].X).ToList(); ;

            // Main Processing
            StringBuilder newtext = new StringBuilder();

            foreach (NormalizedLine line in XSortedList)
            {
                newtext.Append(line.Text);
                newtext.AppendLine();
            }
            // Setting the new Normalized Lines we created.
            layout.Lines = XSortedList;
            // Updating the Text after our processing.
            layout.Text = newtext.ToString();

            return layout;
        }

        private static void processLineBoundingBoxes(AbstractPageLayout layout, string alignment)
        {
            int boxref = 0;

            double Xthresholdratio = 1.0;
            double Ythresholdratio = 1.0;

            List<NormalizedLine> XSortedList = null;

            switch (alignment)
            {
                case LeftAlignment:
                    boxref = 0;
                    Xthresholdratio = 1.0;
                    Ythresholdratio = 1.0;

                    //Adjustment for bullet list text. 
                    foreach (NormalizedLine line in layout.Lines)
                    {
                        if (line.Text.StartsWith(". "))
                        {
                            line.BoundingBox[boxref].X = line.BoundingBox[boxref].X + ImageTextBoxingBulletListAdjustment;
                        }
                    }
                    XSortedList = layout.Lines.Where(o => o.Merged == false).OrderBy(o => o.BoundingBox[boxref].X).ToList();
                    break;
                case RightAlignment:
                    boxref = 1;
                    Xthresholdratio = 1.5;
                    Ythresholdratio = 1.0;

                    XSortedList = layout.Lines.Where(o => o.Merged == false).OrderByDescending(o => o.BoundingBox[boxref].X).ToList();
                    break;
                case CenteredAlignment:
                    boxref = 1;
                    Xthresholdratio = 1.5;
                    Ythresholdratio = 1.0;

                    // Calculate the x1-x2 and sort on this
                    XSortedList = layout.Lines.Where(o => o.Merged == false).OrderBy(o => o.XMedian).ToList();
                    break;
                default:
                    break;
            }

            List<List<NormalizedLine>> regions = new List<List<NormalizedLine>>();

            //Default Region
            regions.Add(new List<NormalizedLine>());
            double regionx = 0;
            int regionidx = 0;

            //First Pass on the X Axis 
            foreach (NormalizedLine line in XSortedList)
            {
                double xcurrent = line.BoundingBox[boxref].X;

                if (alignment.Equals(CenteredAlignment))
                {
                    xcurrent = line.XMedian;
                }

                if (regionx == 0)
                {
                    regions[regionidx].Add(line);
                    regionx = xcurrent;
                }
                //can be improved by testing the upper X boundaries eventually
                else if (xcurrent >= regionx - Xthresholdratio * ImageTextBoxingXThreshold && xcurrent <= regionx + Xthresholdratio * ImageTextBoxingXThreshold)
                {
                    regions[regionidx].Add(line);

                    if (!alignment.Equals(CenteredAlignment))
                    {
                        regionx = (xcurrent + regionx) / 2;
                    }
                }
                else
                {
                    // Add new region 
                    regions.Add(new List<NormalizedLine>());
                    regionidx++;
                    regions[regionidx].Add(line);
                    regionx = xcurrent;
                }
            }

            //Second Pass on the Y Axis 
            foreach (List<NormalizedLine> lines in regions)
            {
                List<NormalizedLine> YSortedList = lines.OrderBy(o => o.BoundingBox[boxref].Y).ToList();

                // the entries are now sorted ascending their Y axis
                double regiony = 0;

                NormalizedLine prevline = null;

                foreach (NormalizedLine line in YSortedList)
                {
                    //Top Left Y
                    double ycurrent = line.BoundingBox[boxref].Y;

                    if (regiony == 0)
                    {
                        prevline = line;
                    }
                    else if (ycurrent >= regiony - Ythresholdratio * ImageTextBoxingYThreshold && ycurrent <= regiony + Ythresholdratio * ImageTextBoxingYThreshold)
                    {
                        line.Merged = true;

                        //Merge current box with previous 
                        prevline.Text += " " + line.Text;

                        //Merge the BoundingBox coordinates
                        switch (alignment)
                        {
                            case LeftAlignment:
                                // Max X 
                                prevline.BoundingBox[1].X = Math.Max(prevline.BoundingBox[1].X, line.BoundingBox[1].X);
                                prevline.BoundingBox[2] = line.BoundingBox[2];
                                prevline.BoundingBox[3].Y = line.BoundingBox[3].Y;
                                break;
                            case RightAlignment:
                                // Min X 
                                prevline.BoundingBox[0].X = Math.Min(prevline.BoundingBox[0].X, line.BoundingBox[0].X);
                                prevline.BoundingBox[3] = line.BoundingBox[3];
                                prevline.BoundingBox[2].Y = line.BoundingBox[2].Y;
                                break;
                            case CenteredAlignment:
                                // Min X 
                                prevline.BoundingBox[0].X = Math.Min(prevline.BoundingBox[0].X, line.BoundingBox[0].X);
                                // Max X 
                                prevline.BoundingBox[1].X = Math.Max(prevline.BoundingBox[1].X, line.BoundingBox[1].X);
                                // Max X / Max Y
                                prevline.BoundingBox[2].X = Math.Max(prevline.BoundingBox[2].X, line.BoundingBox[2].X);
                                prevline.BoundingBox[2].Y = Math.Max(prevline.BoundingBox[2].Y, line.BoundingBox[2].Y);
                                // Min X / Max Y
                                prevline.BoundingBox[3].X = Math.Min(prevline.BoundingBox[3].X, line.BoundingBox[3].X);
                                prevline.BoundingBox[3].Y = Math.Max(prevline.BoundingBox[3].Y, line.BoundingBox[3].Y);

                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        prevline = line;
                    }

                    //Take the bottom left Y axis as new reference
                    regiony = line.BoundingBox[3].Y;
                }
            }
        }
    }
}
