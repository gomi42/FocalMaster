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

using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Media;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace FocalCompiler
{
    internal class PdfBarcodeGenerator : BarcodeGenerator
    {
        private const double mmToPt = 72 / 2.54 / 10;
        
        private const double TopBorder = 10 * mmToPt;
        private const double LeftBorder = 20 * mmToPt;

        private const double ZeroBarWidth = 0.5 * mmToPt;
        private const double OneBarWidth = 2 * ZeroBarWidth;
        private const double BarGapWidth = ZeroBarWidth;
        private const double BarHeight = 9.0 * mmToPt;
        private const double BarGapHeight = 1 * mmToPt;


        /////////////////////////////////////////////////////////////

        private string filename;
        private double currentY;
        private double currentX;
        private int currentPage;

        private PdfDocument document;
        private PdfPage page;
        private XGraphics grafics;
        private XFont pageHeaderFont;
        private XFont rowHeaderFont;
        private XForm[] nibblePatterns;
        private XForm startPattern;
        private XForm endPattern;
        private double rowHeight;

        /////////////////////////////////////////////////////////////

        public PdfBarcodeGenerator()
        {
        }

        /////////////////////////////////////////////////////////////

        public bool GeneratePdf(string focal, string pdfFilename)
        {
            filename = pdfFilename;
            currentPage = 1;

            return Generate(focal, false);
        }

        /////////////////////////////////////////////////////////////

        protected override void Save()
        {
            if (document == null)
            {
                return;
            }

            ClosePage();
            document.Save(filename);
            document.Dispose();
        }

        /////////////////////////////////////////////////////////////

        private void ClosePage()
        {
            if (document == null || page == null)
            {
                return;
            }

            grafics.Dispose();
            page = null;
        }

        /////////////////////////////////////////////////////////////

        private void AddPage()
        {
            currentY = TopBorder;

            if (document == null)
            {
                InitDocument();
            }

            page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;

            grafics = XGraphics.FromPdfPage(page);

            string s = string.Format("Page {0}  {1}", currentPage, Path.GetFileName(filename));
            grafics.DrawString(s, pageHeaderFont, XBrushes.Black, LeftBorder, currentY, XStringFormats.TopLeft);

            currentY += pageHeaderFont.Height + 8 * mmToPt;
        }

        /////////////////////////////////////////////////////////////

        private void InitDocument()
        {
            document = new PdfDocument();
            document.Info.Title = Path.GetFileNameWithoutExtension(filename);
            document.Info.Author = "Focal Master";
            document.Info.Creator = "Focal Master";

            pageHeaderFont = new XFont("Arial", 10, XFontStyle.Regular);
            rowHeaderFont = new XFont("Arial", 9, XFontStyle.Regular);

            CreateNibblePatterns();
        }

        /////////////////////////////////////////////////////////////

        protected override void BeginBarcodeRow(int currentRow, int fromLine, int toLine)
        {
            if (page == null)
            {
                AddPage();
            }

            string s = string.Format("Row {0} ({1} - {2})", currentRow, fromLine, toLine);
            grafics.DrawString(s,
                               rowHeaderFont,
                               XBrushes.Black,
                               LeftBorder,
                               currentY,
                               XStringFormats.TopLeft);
            rowHeight = rowHeaderFont.Height;
            currentY += rowHeight;
            currentX = LeftBorder;
        }

        /////////////////////////////////////////////////////////////

        protected override void EndBarcodeRow()
        {
            var patternHeight = endPattern.PointHeight + BarGapHeight;
            rowHeight += patternHeight;
            currentY += patternHeight;

            if (currentY + rowHeight > page.Height - TopBorder)
            {
                ClosePage();
                currentPage++;
            }
        }

        /////////////////////////////////////////////////////////////

        protected override void AddZeroZeroBar()
        {
            grafics.DrawImage(startPattern, currentX, currentY);
            currentX += startPattern.PointWidth + BarGapWidth;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddOneZeroBar()
        {
            grafics.DrawImage(endPattern, currentX, currentY);
            currentX += endPattern.PointWidth + BarGapWidth;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddBars(byte[] barcode)
        {
            int barcodeLen = barcode.Length;

            for (int i = 0; i < barcodeLen; i++)
            {
                byte b = barcode[i];

                var nibblePattern = nibblePatterns[b >> 4];
                grafics.DrawImage(nibblePattern, currentX, currentY);
                currentX += nibblePattern.PointWidth + BarGapWidth;

                nibblePattern = nibblePatterns[b & 0x0F];
                grafics.DrawImage(nibblePattern, currentX, currentY);
                currentX += nibblePattern.PointWidth + BarGapWidth;
            }
        }

        /////////////////////////////////////////////////////////////

        private void CreateNibblePatterns()
        {
            const int BitsPerNibble = 4;

            startPattern = new XForm(document, XUnit.FromPoint(2 * ZeroBarWidth + BarGapWidth), XUnit.FromPoint(BarHeight));
            XGraphics formGfx = XGraphics.FromForm(startPattern);
            formGfx.DrawRectangle(XBrushes.Black, 0, 0, ZeroBarWidth, BarHeight);
            formGfx.DrawRectangle(XBrushes.Black, ZeroBarWidth + BarGapWidth, 0, ZeroBarWidth, BarHeight);
            formGfx.Dispose();

            endPattern = new XForm(document, XUnit.FromPoint(ZeroBarWidth + OneBarWidth + BarGapWidth), XUnit.FromPoint(BarHeight));
            formGfx = XGraphics.FromForm(endPattern);
            formGfx.DrawRectangle(XBrushes.Black, 0, 0, OneBarWidth, BarHeight);
            formGfx.DrawRectangle(XBrushes.Black, OneBarWidth + ZeroBarWidth, 0, ZeroBarWidth, BarHeight);
            formGfx.Dispose();

            nibblePatterns = new XForm[1 << BitsPerNibble];
            double x;

            for (int i = 0; i < (1 << BitsPerNibble); i++)
            {
                int nibble = i;
                int numberOneBars = 0;

                while (nibble != 0)
                {
                    nibble &= nibble - 1; // this clears the LSB-most set bit
                    numberOneBars++;
                }

                nibble = i;
                var width = (BitsPerNibble - numberOneBars) * ZeroBarWidth + numberOneBars * OneBarWidth + (BitsPerNibble - 1) * BarGapWidth;
                XForm pattern = new XForm(document, XUnit.FromPoint(width), XUnit.FromPoint(BarHeight));
                formGfx = XGraphics.FromForm(pattern);
                x = 0;

                for (int j = 0; j < BitsPerNibble; j++)
                {
                    if ((nibble & 0x08) != 0)
                    {
                        formGfx.DrawRectangle(XBrushes.Black, x, 0, OneBarWidth, BarHeight);
                        x += OneBarWidth + BarGapWidth;
                    }
                    else
                    {
                        formGfx.DrawRectangle(XBrushes.Black, x, 0, ZeroBarWidth, BarHeight);
                        x += ZeroBarWidth + BarGapWidth;
                    }

                    nibble <<= 1;
                }

                formGfx.Dispose();
                nibblePatterns[i] = pattern;
            }
        }
    }
}
