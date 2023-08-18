//
// Author:
//   Michael Göricke
//
// Copyright (c) 2023
//
// This file is part of FocalMaster.
//
// The FocalMaster is free software: you can redistribute it and/or modify
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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace FocalCompiler
{
    internal class DrawingVisualBarcodeGenerator : BarcodeGenerator
    {
        private const double TopBorder = 0;
        private const double LeftBorder = 0;
        private const double ZeroBarWidth = 2;
        private const double OneBarWidth = 2 * ZeroBarWidth;
        private const double GapBarWidth = ZeroBarWidth;
        private const double BarHeight = 33;
        private const double BarGap = 3;

        /////////////////////////////////////////////////////////////

        private DrawingVisual drawingVisual;
        private DrawingContext drawingContext;
        private double currentY;
        private double currentX;

        /////////////////////////////////////////////////////////////

        public bool GenerateVisual(string focal, out DrawingVisual visual)
        {
            var result = Generate(focal, false);
            visual = drawingVisual;

            return result;
        }

        /////////////////////////////////////////////////////////////

        protected override void Save()
        {
            if (drawingVisual == null)
            {
                return;
            }

            drawingContext.Close();
        }

        /////////////////////////////////////////////////////////////

        private void InitVisual()
        {
            currentY = TopBorder;

            drawingVisual = new DrawingVisual();
            drawingContext = drawingVisual.RenderOpen();
        }

        /////////////////////////////////////////////////////////////

        protected override void BeginBarcodeRow(int currentRow, int fromLine, int toLine)
        {
            if (drawingVisual == null)
            {
                InitVisual();
            }

            string s = string.Format("Row {0} ({1} - {2})", currentRow, fromLine, toLine);
            FormattedText text = new FormattedText(s,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    12,
                    Brushes.Black,
                    1);

            drawingContext.DrawText(text, new Point(LeftBorder, currentY));
            currentY += text.Height;
            currentX = LeftBorder;
        }

        /////////////////////////////////////////////////////////////

        protected override void EndBarcodeRow()
        {
            currentY += BarHeight + BarGap;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddZeroBar()
        {
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(currentX, currentY, ZeroBarWidth, BarHeight));

            currentX += ZeroBarWidth + GapBarWidth;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddOneBar()
        {
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(currentX, currentY, OneBarWidth, BarHeight));

            currentX += OneBarWidth + GapBarWidth;
        }
    }

#if AlternativeImplementationUsingDrawing
    internal class DrawingBarcodeGenerator : BarcodeGenerator
    {
        private const double TopBorder = 0;
        private const double LeftBorder = 0;
        private const double ZeroBarWidth = 2;
        private const double OneBarWidth = 2 * ZeroBarWidth;
        private const double GapBarWidth = ZeroBarWidth;
        private const double BarHeight = 33;
        private const double BarGap = 3;

        /////////////////////////////////////////////////////////////

        private DrawingGroup drawingGroup;
        private GlyphTypeface glyphTypeface;
        private double currentY;

        /////////////////////////////////////////////////////////////

        public Drawing Drawing
        {
            get => drawingGroup;
        }

        /////////////////////////////////////////////////////////////

        public bool GenerateDrawing(string focal, out Drawing geometry)
        {
            var result = Generate(focal, false);
            geometry = drawingGroup;

            return result;
        }

        /////////////////////////////////////////////////////////////

        protected override void Save()
        {
        }

        /////////////////////////////////////////////////////////////

        private void InitGeometryGroup()
        {
            currentY = TopBorder;

            drawingGroup = new DrawingGroup();
            Typeface typeface = new Typeface("Arial");

            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                glyphTypeface = null;
            }
        }

        /////////////////////////////////////////////////////////////

        private double AddZeroBar(double x)
        {
            GeometryDrawing geometryDrawing = new GeometryDrawing();
            geometryDrawing.Geometry = new RectangleGeometry(new Rect(x, currentY, ZeroBarWidth, BarHeight));
            geometryDrawing.Brush = Brushes.Black;
            drawingGroup.Children.Add(geometryDrawing);

            return ZeroBarWidth + GapBarWidth;
        }

        /////////////////////////////////////////////////////////////

        private double AddOneBar(double x)
        {
            GeometryDrawing geometryDrawing = new GeometryDrawing();
            geometryDrawing.Geometry = new RectangleGeometry(new Rect(x, currentY, OneBarWidth, BarHeight));
            geometryDrawing.Brush = Brushes.Black;
            drawingGroup.Children.Add(geometryDrawing);

            return OneBarWidth + GapBarWidth;
        }

        /////////////////////////////////////////////////////////////

        private void AddText1(string s)
        {
            FormattedText text;
            text = new FormattedText(s,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    12,
                    Brushes.Black,
                    1);
            var geometryDrawing = new GeometryDrawing();
            geometryDrawing.Geometry = text.BuildGeometry(new Point(LeftBorder, currentY));
            geometryDrawing.Brush = Brushes.Black;

            var drawing = geometryDrawing;
            drawingGroup.Children.Add(drawing);

            currentY += text.Height;
        }

        private void AddText(string text)
        {
            const double FontSize = 12;

            if (glyphTypeface == null)
            {
                return;
            }

            double textWidth = 0;
            var glyphIndexes = new ushort[text.Length];
            var advanceWidths = new double[text.Length];

            for (int ix = 0; ix < text.Length; ix++)
            {
                ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[ix]];
                glyphIndexes[ix] = glyphIndex;

                double width = glyphTypeface.AdvanceWidths[glyphIndex] * FontSize;
                advanceWidths[ix] = width;

                textWidth += width;
            }

            var glyphRun = new GlyphRun(
                glyphTypeface,    // typeface
                0,       // Bi-directional nesting level
                false,   // isSideways
                FontSize,      // pt size
                1, // pixels per dip
                glyphIndexes,   // glyphIndices
                new Point(LeftBorder, currentY + glyphTypeface.Baseline * FontSize),           // baselineOrigin
                advanceWidths,  // advanceWidths
                null,    // glyphOffsets
                null,    // characters
                null,    // deviceFontName
                null,    // clusterMap
                null,    // caretStops
                null);   // xmlLanguage

            var drawing = new GlyphRunDrawing(Brushes.Black, glyphRun);
            drawingGroup.Children.Add(drawing);

            currentY += glyphTypeface.Height * FontSize;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddBarcodeRow(byte[] barcode, int currentRow, int fromLine, int toLine)
        {

            if (drawingGroup == null)
            {
                InitGeometryGroup();
            }

            string s = string.Format("Row {0} ({1} - {2})", currentRow, fromLine, toLine);
            AddText(s);

            int barcodeLen = barcode.Length;
            double x = LeftBorder;

            x += AddZeroBar(x);
            x += AddZeroBar(x);

            for (int i = 0; i < barcodeLen; i++)
            {
                byte b = barcode[i];

                for (int j = 0; j < 8; j++)
                {
                    if ((b & 0x80) == 0x80)
                    {
                        x += AddOneBar(x);
                    }
                    else
                    {
                        x += AddZeroBar(x);
                    }

                    b <<= 1;
                }
            }

            x += AddOneBar(x);
            x += AddZeroBar(x);

            currentY += BarHeight + BarGap;
        }
    }
#endif
}
