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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
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

        /////////////////////////////////////////////////////////////

        public DrawingVisual Visual
        {
            get => drawingVisual;
        }

        /////////////////////////////////////////////////////////////

        public DrawingVisualBarcodeGenerator()
        {
        }

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

        private double AddZeroBar(double x)
        {
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(x, currentY, ZeroBarWidth, BarHeight));

            return ZeroBarWidth + GapBarWidth;
        }

        /////////////////////////////////////////////////////////////

        private double AddOneBar(double x)
        {
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(x, currentY, OneBarWidth, BarHeight));

            return OneBarWidth + GapBarWidth;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddBarcodeRow(byte[] barcode, int currentRow, int fromLine, int toLine)
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

            drawingContext.DrawText(text, new System.Windows.Point(LeftBorder, currentY));
            currentY += text.Height;

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
}
