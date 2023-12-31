﻿//
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

using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FocalCompiler
{
    internal abstract class BitmapBarcodeGenerator : BarcodeGenerator
    {
        private const int PrintAtDpi = 300;
        private const double mmToPrintPx = PrintAtDpi / 25.4;
        private const int WpfDpi = 96;

        private const int ImageWidthPx = (int)(210 * mmToPrintPx);
        private const int ImageHeightPx = (int)(297 * mmToPrintPx);

        private const double DpiToWpf = (double)PrintAtDpi / (double)WpfDpi;

        private const int ImageHeightWpf = (int)(ImageHeightPx / DpiToWpf) + 1;
        private const int ImageWidthWpf = (int)(ImageWidthPx / DpiToWpf) + 1;

        private const double TopBorder = (int)(10 * mmToPrintPx / DpiToWpf);
        private const double LeftBorder = (int)(10 * mmToPrintPx / DpiToWpf);
        private const double ZeroBarWidth = 2;
        private const double OneBarWidth = 2 * ZeroBarWidth;
        private const double GapBarWidth = ZeroBarWidth;
        private const double BarHeight = 33;

        /////////////////////////////////////////////////////////////

        private string filename;
        private string printFilename;
        private DrawingVisual drawingVisual;
        private DrawingContext drawingContext;
        private int currentPage;
        private double currentY;
        private double currentX;
        private double lastRowHeaderHeight;

        /////////////////////////////////////////////////////////////

        public bool GenerateImage(string focal, string outputBaseFilename)
        {
            filename = outputBaseFilename;
            printFilename = Path.GetFileName(outputBaseFilename);
            currentPage = 1;

            return Generate(focal, false);
        }

        /////////////////////////////////////////////////////////////

        protected override void Save()
        {
            if (drawingVisual == null)
            {
                return;
            }

            drawingContext.Close();

            RenderTargetBitmap targetBitmap = new RenderTargetBitmap(ImageWidthPx, ImageHeightPx, PrintAtDpi, PrintAtDpi, PixelFormats.Default);
            targetBitmap.Render(drawingVisual);
            targetBitmap.Freeze();

            BitmapEncoder enc = GetEncoder();
            enc.Frames.Add(BitmapFrame.Create(targetBitmap));

            var baseFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
            FileStream fs = new FileStream(baseFilename + "-" + currentPage.ToString() + Path.GetExtension(filename), FileMode.Create);
            enc.Save(fs);
            fs.Flush();
            fs.Close();
            fs.Dispose();
        
            drawingVisual = null;
        }

        /////////////////////////////////////////////////////////////

        protected abstract BitmapEncoder GetEncoder();

        /////////////////////////////////////////////////////////////

        private void InitImage ()
        {
            currentY = TopBorder;

            drawingVisual = new DrawingVisual ();
            drawingContext = drawingVisual.RenderOpen ();
            drawingContext.DrawRectangle (Brushes.White, null, new Rect (0, 0, ImageWidthWpf, ImageHeightWpf));

            string s = string.Format ("Page {0}  {1}", currentPage, printFilename);
            FormattedText text = new FormattedText(s,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface ("Arial"),
                    12,
                    Brushes.Black,
                    1);

            drawingContext.DrawText (text, new Point (LeftBorder, currentY));
            currentY += (int)(text.Height * 2);
        }

        /////////////////////////////////////////////////////////////

        protected override void BeginBarcodeRow(int currentRow, int fromLine, int toLine)
        {
            if (drawingVisual == null)
            {
                InitImage();
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
            lastRowHeaderHeight = text.Height;
            currentY += lastRowHeaderHeight;
            currentX = LeftBorder;
        }

        /////////////////////////////////////////////////////////////

        protected override void EndBarcodeRow()
        {
            currentY += BarHeight;

            if (currentY + lastRowHeaderHeight + BarHeight > ImageHeightWpf - TopBorder)
            {
                Save();
                currentPage++;
            }
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
}
