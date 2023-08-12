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

using System.IO;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace FocalCompiler
{
    internal class PdfBarcodeGenerator : BarcodeGenerator
    {
        private const double mmToPt = 72 / 2.54 / 10;
        
        private const double TopBorder = 10 * mmToPt;
        private const double LeftBorder = 20 * mmToPt;

        private const double BarGapWidth = 0.5 * mmToPt;
        private const double ZerorBarWidth = 0.5 * mmToPt;
        private const double OneBarWidth = 2 * ZerorBarWidth;
        private const double Barheight = 9.0 * mmToPt;
        private const double BarGapHeight = 1 * mmToPt;


        /////////////////////////////////////////////////////////////

        private string filename;
        private double currentY;
        private int currentPage = 1;

        private PdfDocument document;
        private PdfPage page;
        private XGraphics grafics;
        private XFont pageHeaderFont;
        private XFont rowHeaderFont;
        private XForm[] nibblePatterns;
        private XForm startPattern;
        private XForm endPattern;

        /////////////////////////////////////////////////////////////

        public PdfBarcodeGenerator()
        {
        }

        /////////////////////////////////////////////////////////////

        public bool GeneratePdf(string focal, string pdfFilename)
        {
            filename = pdfFilename;

            return Generate(focal, false);
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

        private void CreateNibblePatterns()
        {
            const int BitsPerNibble = 4;

            startPattern = new XForm(document, XUnit.FromPoint(2 * ZerorBarWidth + BarGapWidth), XUnit.FromPoint(Barheight));
            XGraphics formGfx = XGraphics.FromForm(startPattern);
            formGfx.DrawRectangle(XBrushes.Black, 0, 0, ZerorBarWidth, Barheight);
            formGfx.DrawRectangle(XBrushes.Black, ZerorBarWidth + BarGapWidth, 0, ZerorBarWidth, Barheight);
            formGfx.Dispose();

            endPattern = new XForm(document, XUnit.FromPoint(ZerorBarWidth + OneBarWidth + BarGapWidth), XUnit.FromPoint(Barheight));
            formGfx = XGraphics.FromForm(endPattern);
            formGfx.DrawRectangle(XBrushes.Black, 0, 0, OneBarWidth, Barheight);
            formGfx.DrawRectangle(XBrushes.Black, OneBarWidth + ZerorBarWidth, 0, ZerorBarWidth, Barheight);
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
                var width = (BitsPerNibble - numberOneBars) * ZerorBarWidth + numberOneBars * OneBarWidth + (BitsPerNibble - 1) * BarGapWidth;
                XForm pattern = new XForm(document, XUnit.FromPoint(width), XUnit.FromPoint(Barheight));
                formGfx = XGraphics.FromForm(pattern);
                x = 0;

                for (int j = 0; j < BitsPerNibble; j++)
                {
                    if ((nibble & 0x08) != 0)
                    {
                        formGfx.DrawRectangle(XBrushes.Black, x, 0, OneBarWidth, Barheight);
                        x += OneBarWidth + BarGapWidth;
                    }
                    else
                    {
                        formGfx.DrawRectangle(XBrushes.Black, x, 0, ZerorBarWidth, Barheight);
                        x += ZerorBarWidth + BarGapWidth;
                    }

                    nibble <<= 1;
                }

                formGfx.Dispose();
                nibblePatterns[i] = pattern;
            }
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

        protected override void AddBarcodeRow(byte[] barcode, int currentRow, int fromLine, int toLine)
        {
            double rowHeight;

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

            int barcodeLen = barcode.Length;
            double x = LeftBorder;

            grafics.DrawImage(startPattern, x, currentY);
            x += startPattern.PointWidth + BarGapWidth;

            for (int i = 0; i < barcodeLen; i++)
            {
                byte b = barcode[i];

                var nibblePattern = nibblePatterns[b >> 4];
                grafics.DrawImage(nibblePattern, x, currentY);
                x += nibblePattern.PointWidth + BarGapWidth;

                nibblePattern = nibblePatterns[b & 0x0F];
                grafics.DrawImage(nibblePattern, x, currentY);
                x += nibblePattern.PointWidth + BarGapWidth;
            }

            grafics.DrawImage(endPattern, x, currentY);

            var patternHeight = endPattern.PointHeight + BarGapHeight;
            rowHeight += patternHeight;
            currentY += patternHeight;

            if (currentY + rowHeight > page.Height - TopBorder)
            {
                ClosePage();
                currentPage++;
            }
        }
    }
}
