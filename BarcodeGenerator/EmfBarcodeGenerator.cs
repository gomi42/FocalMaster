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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace FocalCompiler
{
    internal class EmfBarcodeGenerator : BarcodeGenerator
    {
        private const float TopBorder = 0;
        private const float LeftBorder = 0;
        private const float ZeroBarWidth = 2;
        private const float OneBarWidth = 2 * ZeroBarWidth;
        private const float GapBarWidth = ZeroBarWidth;
        private const float BarHeight = 33;
        private const double RowsPerPage = 19;

        /////////////////////////////////////////////////////////////

        private string filename;
        private Metafile metafile;
        private Graphics graphics;
        private int currentPage;
        private int currentRowOnPage;
        private float currentY;
        private float currentX;

        /////////////////////////////////////////////////////////////

        public bool GenerateEmf(string focal, string outputBaseFilename)
        {
            filename = outputBaseFilename;
            currentPage = 1;

            return Generate(focal, false);
        }

        /////////////////////////////////////////////////////////////

        protected override void Save()
        {
            if (graphics == null)
            {
                return;
            }

            graphics.Dispose();
            metafile.Dispose();

            graphics = null;
            metafile = null;
        }

        /////////////////////////////////////////////////////////////

        private void InitImage()
        {
            Bitmap bitmap = new Bitmap(16, 16);
            Graphics referenceGraphics = Graphics.FromImage(bitmap);
            IntPtr referenceHdc = referenceGraphics.GetHdc();

            var baseFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
            metafile = new Metafile(baseFilename + "-" + currentPage.ToString() + Path.GetExtension(filename), referenceHdc, EmfType.EmfOnly);

            referenceGraphics.ReleaseHdc(referenceHdc);
            referenceGraphics.Dispose();
            bitmap.Dispose();

            graphics = Graphics.FromImage(metafile);

            currentY = TopBorder;
            currentRowOnPage = 0;
        }

        /////////////////////////////////////////////////////////////

        protected override void BeginBarcodeRow(int currentRow, int fromLine, int toLine)
        {
            if (graphics == null)
            {
                InitImage();
            }

            string s = string.Format("Row {0} ({1} - {2})", currentRow, fromLine, toLine);

            Font drawFont = new Font("Arial", 9);
            SolidBrush drawBrush = new SolidBrush(Color.Black);

            graphics.DrawString(s, drawFont, drawBrush, new PointF(LeftBorder, currentY + 3));

            currentY += drawFont.Height + 2;
            currentX = LeftBorder;
        }

        /////////////////////////////////////////////////////////////

        protected override void EndBarcodeRow()
        {
            currentY += BarHeight;
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
            graphics.FillRectangle(Brushes.Black, currentX, currentY, ZeroBarWidth, BarHeight);

            currentX += ZeroBarWidth + GapBarWidth;
        }

        /////////////////////////////////////////////////////////////

        protected override void AddOneBar()
        {
            graphics.FillRectangle(Brushes.Black, currentX, currentY, OneBarWidth, BarHeight);

            currentX += OneBarWidth + GapBarWidth;
        }
    }
}
