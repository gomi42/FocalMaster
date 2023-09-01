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

using System.Drawing;

namespace ShapeConverter.Parser.Pdf
{
    /////////////////////////////////////////////////////////////

    internal abstract class PdfBitmapInfo
    {
        public PdfBitmapInfo(Bitmap bitmap, int pageNumber)
        {
            Bitmap = bitmap;
            PageNumber = pageNumber;
        }

        public Bitmap Bitmap { get; }
        public int PageNumber { get; }
    }

    /////////////////////////////////////////////////////////////

    internal class PdfBitmapInfoGraphic : PdfBitmapInfo
    {
        public PdfBitmapInfoGraphic(Bitmap bitmap, int pageNumber)
                  : base(bitmap, pageNumber)
        {
        }
    }

    /////////////////////////////////////////////////////////////

    internal class PdfBitmapInfoImage : PdfBitmapInfo
    {
        public PdfBitmapInfoImage(Bitmap bitmap, int pageNumber, int imageNumber)
                  : base(bitmap, pageNumber)
        {
            ImageNumber = imageNumber;
        }

        public int ImageNumber { get; }
    }
}

