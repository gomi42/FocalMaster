#define TEST
//
// Author:
//   Michael Göricke
//
// Copyright (c) 2023
//
// This file is part of FocalMaster.
//
// ShapeConverter is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see<http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.IO;
using ShapeConverter.BusinessLogic.Generators;
using ShapeConverter.BusinessLogic.Parser.Pdf.Main;
using ShapeConverter.Helper;

namespace ShapeConverter.Parser.Pdf
{
    /// <summary>
    /// The PDF parser
    /// </summary>
    internal class PdfParser
    {
        /// <summary>
        /// Parse the given file and convert it to a list of graphic paths
        /// </summary>
        public IEnumerable<Bitmap> Parse(string filename)
        {
            PdfDocument inputDocument = PdfReader.Open(filename);
            var invisibleGroups = GetVisibleGroups(inputDocument.OCPropperties);

            foreach (PdfPage page in inputDocument.Pages)
            {
                PdfDictionary resources = page.Elements.GetDictionary(PdfKeys.Resources);
                var (geometry, images) = Parse(page, invisibleGroups);

                if (images.Count > 0)
                {
                    foreach (var image in images)
                    {
                        yield return PdfDictionaryExtensions.ToImage(image);
                    }
                }
                else
                {
                    var drawingVisual = DrawingVisualGenerator.Generate(geometry);

                    var (left, bottom) = GetBounds(drawingVisual);
                    left += 10;
                    bottom += 10;

                    var back = new DrawingVisual();
                    var ctx = back.RenderOpen();
                    ctx.DrawRectangle(System.Windows.Media.Brushes.White, null, new System.Windows.Rect(0, 0, left, bottom));
                    ctx.Close();

                    back.Children.Add(drawingVisual);

                    double scaleFactor = 1500 / left;
                    RenderTargetBitmap targetBitmap = new RenderTargetBitmap((int)(left * scaleFactor), (int)(bottom * scaleFactor), 96 * scaleFactor, 96 * scaleFactor, PixelFormats.Default);
                    targetBitmap.Render(back);
                    targetBitmap.Freeze();

                    MemoryStream stream = new MemoryStream();
                    BitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(targetBitmap));
                    encoder.Save(stream);

                    yield return new Bitmap(stream);
                }
            }

            CommonHelper.CleanUpTempDir();
        }

        /// <summary>
        /// Get the bounds of a visual
        /// </summary>
        private (double, double) GetBounds(DrawingVisual drawingVisual)
        {
            double left = 0;
            double bottom = 0;

            void CalcBounds(DrawingVisual child)
            {
                var bounds = ((DrawingVisual)child).ContentBounds;

                if (!bounds.IsEmpty)
                {
                    if (bounds.Left > left)
                    {
                        left = bounds.Left;
                    }

                    if (bounds.Bottom > bottom)
                    {
                        bottom = bounds.Bottom;
                    }
                }

                foreach (var child2 in child.Children)
                {
                    CalcBounds((DrawingVisual)child2);
                }
            }

            CalcBounds(drawingVisual);

            return (left, bottom);
        }

        /// <summary>
        /// Parse a single PDF page
        /// </summary>
        private (GraphicVisual, List<PdfDictionary>) Parse(PdfPage page, PdfDictionary[] invisibleGroups)
        {
            var currentGraphicsState = new GraphicsState();
            currentGraphicsState.TransformationMatrix = Matrix.Identity;

            var mediaBox = page.MediaBox;
            currentGraphicsState.Mirror = new Matrix(1, 0, 0, -1, 0, mediaBox.Y2);

            var sequence = ContentReader.ReadContent(page);

            var interpreter = new ContentInterpreter();
            return interpreter.Run(page, sequence, currentGraphicsState, invisibleGroups);
        }

        /// <summary>
        /// Get all invisible groups
        /// </summary>
        private PdfDictionary[] GetVisibleGroups(PdfDictionary prop)
        {
            if (prop == null)
            {
                return null;
            }

            var visibleGroups = new List<PdfDictionary>();
            var ocgs = prop.Elements.GetArray(PdfKeys.OCGs);

            var d = prop.Elements.GetDictionary(PdfKeys.D);
            var baseState = d.Elements.GetName(PdfKeys.BaseState);

            if (string.IsNullOrEmpty(baseState) || baseState == PdfKeys.ON)
            {
                for (int i = 0; i < ocgs.Elements.Count; i++)
                {
                    visibleGroups.Add(ocgs.Elements.GetDictionary(i));
                }
            }

            var on = d.Elements.GetArray(PdfKeys.ON);

            if (on != null)
            {
                for (int i = 0; i < on.Elements.Count; i++)
                {
                    var dict = on.Elements.GetDictionary(i);

                    if (!visibleGroups.Contains(dict))
                    {
                        visibleGroups.Add(dict);
                    }
                }
            }

            var off = d.Elements.GetArray(PdfKeys.OFF);

            if (off != null)
            {
                for (int i = 0; i < off.Elements.Count; i++)
                {
                    var dict = off.Elements.GetDictionary(i);

                    if (visibleGroups.Contains(dict))
                    {
                        visibleGroups.Remove(dict);
                    }
                }
            }

            return visibleGroups.ToArray();
        }
    }
}

