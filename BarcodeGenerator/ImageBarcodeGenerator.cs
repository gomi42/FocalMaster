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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FocalCompiler
{
    class ImageBarcodeGenerator : BarcodeGenerator
    {
        private const int PrintAtDpi = 300;
        private const int WpfDpi = 96;

        private const int ImageWidthPx = 2121;
        private const int ImageHeightPx = 3000;

        private const double DpiToWpf = (double)PrintAtDpi / (double)WpfDpi;

        private const int ImageHeightWpf = (int)(ImageHeightPx / DpiToWpf) + 1;
        private const int ImageWidthWpf = (int)(ImageWidthPx / DpiToWpf) + 1;

        private const double TopBorder = 10;
        private const double LeftBorder = 10;
        private const double ZeroBarWidth = 2;
        private const double OneBarWidth = 2 * ZeroBarWidth;
        private const double GapBarWidth = ZeroBarWidth;
        private const double BarHeight = 33;

        /////////////////////////////////////////////////////////////

        private DateTime printDate;
        private DrawingVisual drawingVisual;
        private DrawingContext drawingContext;
        private double currentY;
        private int currentPage = 1;

        /////////////////////////////////////////////////////////////

        public string ImageBaseFilename
        {
            get;
            private set;
        }

        /////////////////////////////////////////////////////////////

        public string PrintFilename
        {
            get;
            private set;
        }

        /////////////////////////////////////////////////////////////

        public ImageBarcodeGenerator ()
        {
            printDate = DateTime.Now;
        }

        /////////////////////////////////////////////////////////////

        public bool GenerateImage(string focal, string outputBaseFilename)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(focal);
            MemoryStream stream = new MemoryStream(byteArray);
            StreamReader reader = new StreamReader(stream);

            return GenerateImage(reader, outputBaseFilename, false);
        }

        /////////////////////////////////////////////////////////////

        public bool GenerateImage(string inputFilename, bool hexDebugOutput)
        {
            /////////////////////////////

            StreamReader inFileStream;

            try
            {
                inFileStream = new StreamReader(inputFilename, System.Text.Encoding.ASCII);
            }
            catch
            {
                Errors.Add(string.Format("Cannot open input file: {0}", inputFilename));
                return false;
            }

            return GenerateImage(inFileStream, inputFilename, hexDebugOutput);
        }

        /////////////////////////////////////////////////////////////

        private bool GenerateImage(StreamReader inFileStream, string outputBaseFilename, bool hexDebugOutput)
        {

            ImageBaseFilename = Path.Combine(Path.GetDirectoryName(outputBaseFilename), Path.GetFileNameWithoutExtension(outputBaseFilename));
            PrintFilename = Path.GetFileName(outputBaseFilename);

            return Generate(inFileStream, hexDebugOutput);
        }

        /////////////////////////////////////////////////////////////

        private void InitImage ()
        {
            currentY = TopBorder;

            drawingVisual = new DrawingVisual ();
            drawingContext = drawingVisual.RenderOpen ();
            drawingContext.DrawRectangle (Brushes.White, null, new Rect (0, 0, ImageWidthWpf, ImageHeightWpf));

            string s = string.Format ("Page {0}  {1}  {2}", currentPage, printDate, PrintFilename);
            FormattedText text = new FormattedText(s,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface ("Arial"),
                    12,
                    Brushes.Black,
                    1);

            drawingContext.DrawText (text, new System.Windows.Point (LeftBorder, currentY));
            currentY += (int)(text.Height * 2);
        }

        /////////////////////////////////////////////////////////////

        protected override void Save ()
        {
            if (drawingVisual == null)
            {
                return;
            }

            drawingContext.Close ();

            RenderTargetBitmap targetBitmap = new RenderTargetBitmap (ImageWidthPx, ImageHeightPx, PrintAtDpi, PrintAtDpi, PixelFormats.Default);
            targetBitmap.Render (drawingVisual);
            targetBitmap.Freeze ();

            JpegBitmapEncoder enc = new JpegBitmapEncoder ();
            enc.QualityLevel = 80;
            enc.Frames.Add (BitmapFrame.Create (targetBitmap));

            string filename = ImageBaseFilename + "-" + currentPage.ToString () + ".jpg";
            FileStream fs = new FileStream (filename, FileMode.Create);
            enc.Save (fs);
            fs.Flush ();
            fs.Close ();
            fs.Dispose();
        }

        /////////////////////////////////////////////////////////////

        private double AddZeroBar (double x)
        {
            drawingContext.DrawRectangle (Brushes.Black, null, new Rect (x, currentY, ZeroBarWidth, BarHeight));

            return ZeroBarWidth + GapBarWidth;
        }

        /////////////////////////////////////////////////////////////

        private double AddOneBar (double x)
        {
            drawingContext.DrawRectangle (Brushes.Black, null, new Rect (x, currentY, OneBarWidth, BarHeight));

            return OneBarWidth + GapBarWidth;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddBarcode (byte[] barcode, int barcodeLen, int currentRow, int fromLine, int toLine)
        {
            if (drawingVisual == null)
            {
                InitImage ();
            }

            string s = string.Format ("Row {0} ({1} - {2})", currentRow, fromLine, toLine);
            FormattedText text = new FormattedText (s,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface ("Arial"),
                    12,
                    Brushes.Black,
                    1);

            drawingContext.DrawText (text, new System.Windows.Point (LeftBorder, currentY));
            currentY += text.Height;

            double x = LeftBorder;

            x += AddZeroBar (x);
            x += AddZeroBar (x);

            for (int i = 0; i < barcodeLen; i++)
            {
                byte b = barcode[i];

                for (int j = 0; j < 8; j++)
                {
                    if ((b & 0x80) == 0x80)
                    {
                        x += AddOneBar (x);
                    }
                    else
                    {
                        x += AddZeroBar (x);
                    }

                    b <<= 1;
                }
            }

            x += AddOneBar (x);
            x += AddZeroBar (x);

            currentY += BarHeight;

            if (currentY + text.Height + BarHeight > ImageHeightWpf - TopBorder)
            {
                Save ();
                drawingVisual = null;
                currentPage++;
            }
        }
    }
}
