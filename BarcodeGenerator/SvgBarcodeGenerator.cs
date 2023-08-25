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

namespace FocalCompiler
{
    internal class SvgBarcodeGenerator : BarcodeGenerator
    {
        // use pixel to avoid half pixels on a monitor
        private const string SvgUnit = "px";

        private const double TopBorder = 0;
        private const double LeftBorder = 0;

        private const double ZeroBarWidth = 3;
        private const double OneBarWidth = 2 * ZeroBarWidth;
        private const double BarGapWidth = ZeroBarWidth;
        private const double BarHeight = 40;
        private const double BarGapHeight = 5;
        private const double FontSize = 14;
        private const double RowsPerPage = 19;

        /////////////////////////////////////////////////////////////

        private string filename;
        private StreamWriter svgWriter;
        private double currentX;
        private double currentY;
        private int currentPage;
        private int currentRowOnPage;
        private double ZeroZeroWidth;
        private double OneZeroWidth;
        private double[] nibbleWidths;

        /////////////////////////////////////////////////////////////

        public bool GenerateSvg(string focal, string outputBaseFilename)
        {
            filename = outputBaseFilename;
            currentPage = 1;

            return Generate(focal, false);
        }

        /////////////////////////////////////////////////////////////

        protected override void Save()
        {
            if (svgWriter == null)
            {
                return;
            }

            svgWriter.WriteLine("</svg>");

            svgWriter.Close();
            svgWriter.Dispose();
            svgWriter = null;
        }

        /////////////////////////////////////////////////////////////

        private void InitImage()
        {
            try
            {
                var baseFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
                svgWriter = new StreamWriter(baseFilename + "-" + currentPage.ToString() + Path.GetExtension(filename));

                svgWriter.WriteLine("<?xml version=\"1.0\" standalone=\"no\"?>");
                svgWriter.WriteLine("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\">");
                svgWriter.WriteLine("<style>");
                svgWriter.WriteLine(string.Format("text {{ font-family:Arial; font-size: {0}; fill:black }}", FormatValueUnit(FontSize)));
                svgWriter.WriteLine(string.Format("rect {{ y:0px; height:{0}; fill:black }}", FormatValueUnit(BarHeight)));
                svgWriter.WriteLine("</style>");
                CreatePatterns();

                currentY = TopBorder;
                currentRowOnPage = 0;
            }
            catch
            {
                return;
            }
        }

        /////////////////////////////////////////////////////////////

        protected override void BeginBarcodeRow(int currentRow, int fromLine, int toLine)
        {
            if (svgWriter == null)
            {
                InitImage();
            }

            string s = string.Format("Row {0} ({1} - {2})", currentRow, fromLine, toLine);
            svgWriter.WriteLine(string.Format("<text x=\"{0}\" y=\"{1}\">{2}</text>", FormatValueUnit(LeftBorder), FormatValueUnit(currentY + FontSize - 2), s));
            currentY += FontSize;
            currentX = LeftBorder;
        }

        /////////////////////////////////////////////////////////////

        protected override void EndBarcodeRow()
        {
            currentY += BarHeight + BarGapHeight;
            currentRowOnPage++;

            if (currentRowOnPage >= RowsPerPage)
            {
                Save();
                currentPage++;
            }
        }

        /////////////////////////////////////////////////////////////
        protected override void AddZeroZeroBar()
        {
            svgWriter.WriteLine(string.Format("<use href=\"#a\" x=\"{0}\" y=\"{1}\" />", FormatValueUnit(currentX), FormatValueUnit(currentY)));
            currentX += ZeroZeroWidth;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddOneZeroBar()
        {
            svgWriter.WriteLine(string.Format("<use href=\"#b\" x=\"{0}\" y=\"{1}\" />", FormatValueUnit(currentX), FormatValueUnit(currentY)));
            currentX += OneZeroWidth;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddBars(byte[] barcode)
        {
            int barcodeLen = barcode.Length;

            for (int i = 0; i < barcodeLen; i++)
            {
                byte b = barcode[i];
                
                var nibble = b >> 4;
                svgWriter.WriteLine(string.Format("<use href=\"#{0}\" x=\"{1}\" y=\"{2}\" />", (char)('c' + nibble), FormatValueUnit(currentX), FormatValueUnit(currentY)));
                currentX += nibbleWidths[nibble];

                nibble = b & 0x0F;
                svgWriter.WriteLine(string.Format("<use href=\"#{0}\" x=\"{1}\" y=\"{2}\" />", (char)('c' + nibble), FormatValueUnit(currentX), FormatValueUnit(currentY)));
                currentX += nibbleWidths[nibble];
            }
        }

        /////////////////////////////////////////////////////////////

        private string FormatValueUnit(double d)
        {
            var strValue = d.ToString("0.#####", CultureInfo.InvariantCulture);

            return strValue + SvgUnit;
        }

        /////////////////////////////////////////////////////////////

        private double AddZeroBar(double x)
        {
            svgWriter.WriteLine(string.Format("<use href=\"#A\" x=\"{0}\" />", FormatValueUnit(x)));

            return ZeroBarWidth + BarGapWidth;
        }

        /////////////////////////////////////////////////////////////

        private double AddOneBar(double x)
        {
            svgWriter.WriteLine(string.Format("<use href=\"#B\" x=\"{0}\" />", FormatValueUnit(x)));

            return OneBarWidth + BarGapWidth;
        }

        /////////////////////////////////////////////////////////////

        private void CreatePatterns()
        {
            const int BitsPerNibble = 4;
            double x;

            svgWriter.WriteLine("<defs>");

            svgWriter.WriteLine("<g id=\"A\">");
            svgWriter.WriteLine(string.Format("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\"/>", FormatValueUnit(0), FormatValueUnit(0), FormatValueUnit(ZeroBarWidth)));
            svgWriter.WriteLine("</g>");
            svgWriter.WriteLine("<g id=\"B\">");
            svgWriter.WriteLine(string.Format("<rect x=\"{0}\" y=\"{1}\" width =\"{2}\"/>", FormatValueUnit(0), FormatValueUnit(0),  FormatValueUnit(OneBarWidth)));
            svgWriter.WriteLine("</g>");

            x = 0;
            svgWriter.WriteLine("<g id=\"a\">");
            x += AddZeroBar(x);
            x += AddZeroBar(x);
            ZeroZeroWidth = x;
            svgWriter.WriteLine("</g>");

            x = 0;
            svgWriter.WriteLine("<g id=\"b\">");
            x += AddOneBar(x);
            x += AddZeroBar(x);
            OneZeroWidth = x;
            svgWriter.WriteLine("</g>");

            nibbleWidths = new double[1 << BitsPerNibble];

            for (int i = 0; i < (1 << BitsPerNibble); i++)
            {
                svgWriter.WriteLine(string.Format("<g id=\"{0}\">", (char)('c' + i)));

                var nibble = i;
                x = 0;

                for (int j = 0; j < BitsPerNibble; j++)
                {
                    if ((nibble & 0x08) != 0)
                    {
                        x += AddOneBar(x);
                    }
                    else
                    {
                        x += AddZeroBar(x);
                    }

                    nibble <<= 1;
                }

                svgWriter.WriteLine("</g>");

                nibbleWidths[i] = x;
            }

            svgWriter.WriteLine("</defs>");
        }
    }

#if ExhaustiveImplementation
    internal class SvgBarcodeGenerator : BarcodeGenerator
    {
        // use pixel to avoid half pixels on a monitor
        private const string SvgUnit = "px";

        private const double TopBorder = 0;
        private const double LeftBorder = 0;

        private const double ZeroBarWidth = 3;
        private const double OneBarWidth = 2 * ZeroBarWidth;
        private const double BarGapWidth = ZeroBarWidth;
        private const double BarHeight = 40;
        private const double BarGapHeight = 5;
        private const double FontSize = 14;
        private const double RowsPerPage = 19;

        /////////////////////////////////////////////////////////////

        private string imageBaseFilename;
        private StreamWriter svgWriter;
        private double currentX;
        private double currentY;
        private int currentPage;
        private int currentRowOnPage;

        /////////////////////////////////////////////////////////////

        public bool GenerateSvg(string focal, string outputBaseFilename)
        {
            imageBaseFilename = Path.Combine(Path.GetDirectoryName(outputBaseFilename), Path.GetFileNameWithoutExtension(outputBaseFilename));
            currentPage = 1;

            return Generate(focal, false);
        }

        /////////////////////////////////////////////////////////////

        protected override void Save()
        {
            if (svgWriter == null)
            {
                return;
            }

            svgWriter.WriteLine("</svg>");

            svgWriter.Close();
            svgWriter.Dispose();
            svgWriter = null;
        }

        /////////////////////////////////////////////////////////////

        private void InitImage()
        {
            try
            {
                svgWriter = new StreamWriter(imageBaseFilename + "-" + currentPage.ToString() + ".svg");

                svgWriter.WriteLine("<?xml version=\"1.0\" standalone=\"no\"?>");
                svgWriter.WriteLine("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\">");
                svgWriter.WriteLine("<style>");
                svgWriter.WriteLine(string.Format("text {{ font-family:Arial; font-size: {0} }}", FormatValueUnit(FontSize)));
                svgWriter.WriteLine("</style>");
                svgWriter.WriteLine("");
            }
            catch
            {
                return;
            }

            currentY = TopBorder;
            currentRowOnPage = 0;
        }

        /////////////////////////////////////////////////////////////

        protected override void BeginBarcodeRow(int currentRow, int fromLine, int toLine)
        {
            if (svgWriter == null)
            {
                InitImage();
            }

            string s = string.Format("Row {0} ({1} - {2})", currentRow, fromLine, toLine);
            svgWriter.WriteLine(string.Format("<text x=\"{0}\" y=\"{1}\" fill=\"black\">{2}</text>", FormatValueUnit(LeftBorder), FormatValueUnit(currentY + FontSize - 2), s));
            currentY += FontSize;
            currentX = LeftBorder;
        }

        /////////////////////////////////////////////////////////////

        protected override void EndBarcodeRow()
        {
            currentY += BarHeight + BarGapHeight;
            currentRowOnPage++;

            if (currentRowOnPage >= RowsPerPage)
            {
                Save();
                currentPage++;
            }
        }

        /////////////////////////////////////////////////////////////

        protected override void AddZeroBar()
        {
            svgWriter.WriteLine(string.Format("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"black\"/>", FormatValueUnit(currentX), FormatValueUnit(currentY), FormatValueUnit(ZeroBarWidth), FormatValueUnit(BarHeight)));

            currentX += ZeroBarWidth + BarGapWidth;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddOneBar()
        {
            svgWriter.WriteLine(string.Format("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"black\"/>", FormatValueUnit(currentX), FormatValueUnit(currentY), FormatValueUnit(OneBarWidth), FormatValueUnit(BarHeight)));

            currentX += OneBarWidth + BarGapWidth;
        }

        /////////////////////////////////////////////////////////////

        private string FormatValueUnit(double d)
        {
            var strValue = d.ToString("0.#####", CultureInfo.InvariantCulture);

            return strValue + SvgUnit;
        }
    }
#endif
}
