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

namespace FocalMaster
{
    /////////////////////////////////////////////////////////////

    internal enum ScannerResultId
    {
        NoBarcodeFound,
        NoProgramCode,
        InvalidSignature,
        CheckSumError,
        ProgramCode,
        CannotOpenFile
    }

    /////////////////////////////////////////////////////////////

    internal abstract class ScannerResult
    {
        public ScannerResult(ScannerResultId scanResult, string filename)
        {
            ScanResult = scanResult;
            Filename = filename;
        }

        public ScannerResultId ScanResult { get; }
        public string Filename { get; }
    }

    /////////////////////////////////////////////////////////////

    internal class ScannerResultError : ScannerResult
    {
        public ScannerResultError(ScannerResultId scanResult, string filename)
                : base(scanResult, filename)
        {
        }
    }

    /////////////////////////////////////////////////////////////

    internal class ScannerResultImage : ScannerResult
    {
        public ScannerResultImage(ScannerResultId scanResult, string filename)
                : base(scanResult, filename)
        {
        }
    }

    /////////////////////////////////////////////////////////////

    internal abstract class ScannerResultPage : ScannerResult
    {
        public ScannerResultPage(ScannerResultId scanResult, string filename, int pageNumber)
                : base(scanResult, filename)
        {
            PageNumber = pageNumber;
        }

        public int PageNumber { get; }
    }

    /////////////////////////////////////////////////////////////

    internal class ScannerResultPageImage : ScannerResultPage
    {
        public ScannerResultPageImage(ScannerResultId scanResult, string filename, int pageNumber, int imageNumber)
            : base(scanResult, filename, pageNumber)
        {
            ImageNumber = imageNumber;
        }

        public int ImageNumber { get; }
    }

    /////////////////////////////////////////////////////////////

    internal class ScannerResultPageGraphic : ScannerResultPage
    {
        public ScannerResultPageGraphic(ScannerResultId scanResult, string filename, int pageNumber)
            : base(scanResult, filename, pageNumber)
        {
        }
    }
}
