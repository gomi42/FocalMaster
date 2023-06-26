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
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Imaging;
using FocalDecompiler;

namespace FocalCompiler
{
    internal enum ScanResult
    {
        NoBarcode = 0,
        NoProgramCode = 1,
        CheckSumError = 2,
        Ok = 3
    }

    internal class ErrorImageData
    {
        public string Filename;
        public byte[,] GrayImage;
        public List<Rectangle> BarcodeAreas;
        public List<ScanResult> AreaResults;
    }

    internal class BarcodeScanner
    {
        enum FindNextBarcodeAreaResult
        {
            Ok,
            NoBarcode,
            AreaTooSmall
        }

        const int ProgramBarcodeHeaderLength = 3;
        const int MinProgramBarcodeLength = ProgramBarcodeHeaderLength + 1;

        /////////////////////////////////////////////////////////////

        public List<string> Errors { get; private set; }

        public ErrorImageData ErrorImageData { get; private set; }

        /////////////////////////////////////////////////////////////

        public string Scan(List<string> files)
        {
            Errors = new List<string>();
            var programCode = new List<byte>();
            int lastCheckSum = 0;
            int currentRow = 0;

            foreach (var file in files)
            {
                var bitmap = (Bitmap)Image.FromFile(file);
                var grayImage = CreateGrayScale(bitmap);
                var binaryImage = Binarize(grayImage);
                var barcodeAreas = FindBarcodeAreas(binaryImage);

                if (barcodeAreas.Count == 0)
                {
                    Errors.Add($"No barcodes found in {file}");
                    return null;
                }

                var check = DecodeBarcodes(binaryImage, barcodeAreas, ref lastCheckSum, ref currentRow, out List<byte> code, out List<ScanResult> areaResults);

                if (check != ScanResult.Ok)
                {
                    switch (check)
                    {
                        case ScanResult.NoBarcode:
                            Errors.Add($"No barcodes found in {file}");
                            break;

                        case ScanResult.NoProgramCode:
                            Errors.Add($"No program barcodes found in {file}");
                            break;

                        case ScanResult.CheckSumError:
                            Errors.Add($"Checksum error in {file}");
                            break;
                    }

                    ErrorImageData = new ErrorImageData { Filename = file, GrayImage = grayImage, BarcodeAreas = barcodeAreas, AreaResults = areaResults };

                    return null;
                }

                programCode.AddRange(code);
            }

            var decomp = new Decompiler();
            decomp.Decompile(programCode, out string focal);

            ErrorImageData = null;

            return focal;
        }

        /////////////////////////////////////////////////////////////

        public List<BitmapSource> ScanDebug(List<string> files)
        {
            Errors = new List<string>();
            int lastCheckSum = 0;
            int currentRow = 0;
            var results = new List<BitmapSource>();

            foreach (var file in files)
            {
                var bitmap = (Bitmap)Image.FromFile(file);
                var grayImage = CreateGrayScale(bitmap);

                var binaryImage = Binarize(grayImage);
                var barcodeAreas = FindBarcodeAreas(binaryImage);

                var check = DecodeBarcodes(binaryImage, barcodeAreas, ref lastCheckSum, ref currentRow, out _, out List<ScanResult> areaResults);

                if (check != ScanResult.Ok)
                {
                    switch (check)
                    {
                        case ScanResult.NoBarcode:
                            Errors.Add($"No barcodes found in {file}");
                            break;

                        case ScanResult.NoProgramCode:
                            Errors.Add($"No program barcodes found in {file}");
                            break;

                        case ScanResult.CheckSumError:
                            Errors.Add($"Checksum error in {file}");
                            break;
                    }

                    results.Add(BitmapSourceConverter.GetBitmapSource(grayImage, barcodeAreas, areaResults));
                    return results;
                }
            }

            return results;
        }

        /////////////////////////////////////////////////////////////

        private List<Rectangle> FindBarcodeAreas(byte[,] blacks)
        {
            var results = new List<Rectangle>();
            var height = blacks.GetLength(1);
            int y = 0;

            while (y < height)
            {
                var ret = FindNextBarcodeArea(blacks, y, out Rectangle rect);

                if (ret == FindNextBarcodeAreaResult.NoBarcode)
                {
                    y++;
                    continue;
                }

                if (ret == FindNextBarcodeAreaResult.Ok)
                {
                    results.Add(rect);
                }

                y = rect.Bottom + 1;
            }

            return results;
        }

        /////////////////////////////////////////////////////////////

        private FindNextBarcodeAreaResult FindNextBarcodeArea(byte[,] blacks, int startY, out Rectangle result)
        {
            var width = blacks.GetLength(0);
            var height = blacks.GetLength(1);
            int minX;
            int maxX;

            if (!IsPlausibleBarcodeRow(blacks, startY, out minX, out maxX))
            {
                result = default;
                return FindNextBarcodeAreaResult.NoBarcode;
            }

            int lastBlackY = startY + 1;

            for (int y = lastBlackY; y < height; y++)
            {
                if (!IsPlausibleBarcodeRow(blacks, y, out int firstX, out int lastX))
                {
                    // allow 3 pixel rows within a barcode to be unreadble, that can 
                    // be caused by rounding of the binarizer
                    if (y > lastBlackY + 3)
                    {
                        break;
                    }
                }
                else
                {
                    lastBlackY = y;

                    if (firstX < minX)
                    {
                        minX = firstX;
                    }

                    if (lastX > maxX)
                    {
                        maxX = lastX;
                    }
                }
            }

            if (lastBlackY - startY < 10)
            {
                result = new Rectangle(0, startY, width, lastBlackY - startY);
                return FindNextBarcodeAreaResult.AreaTooSmall;
            }

            result = new Rectangle(minX, startY, maxX - minX + 1, lastBlackY - startY);
            return FindNextBarcodeAreaResult.Ok;
        }

        /////////////////////////////////////////////////////////////

        private bool IsPlausibleBarcodeRow(byte[,] blacks, int startY, out int firstX, out int lastX)
        {
            var width = blacks.GetLength(0);
            int firstBlackX = -1;

            // find left-most black pixel of that row
            for (int x = 0; x < width; x++)
            {
                if (blacks[x, startY] == 0)
                {
                    firstBlackX = x;
                    break;
                }
            }

            if (firstBlackX == -1)
            {
                firstX = 0;
                lastX = 0;
                return false;
            }

            // find right-most black pixel
            int lastBlackX = width - 1;

            while (lastBlackX > firstBlackX)
            {
                if (blacks[lastBlackX, startY] == 0)
                {
                    break;
                }

                lastBlackX--;
            }

            if (firstBlackX == lastBlackX)
            {
                firstX = 0;
                lastX = 0;
                return false;
            }

            // determine the number of black and white pixels
            int numWhite = 0;
            int numBlack = 0;

            for (int x = firstBlackX; x <= lastBlackX; x++)
            {
                if (blacks[x, startY] == 0)
                {
                    numBlack++;
                }
                else
                {
                    numWhite++;
                }

            }

            firstX = firstBlackX;
            lastX = lastBlackX;

            return numWhite > 10 && numBlack >= numWhite;
        }

        /////////////////////////////////////////////////////////////

        private ScanResult DecodeBarcodes(byte[,] blacks, List<Rectangle> barcodeAreas, ref int lastCheckSum, ref int currentRow, out List<byte> programCode, out List<ScanResult> areaResults)
        {
            programCode = null;
            areaResults = new List<ScanResult>();
            byte[] data;
            var code = new List<byte>();
            ScanResult lastResult = ScanResult.NoBarcode;

            foreach (var area in barcodeAreas)
            {
                var check = DecodeArea(blacks, area, lastCheckSum, currentRow, out data, out int newCheckSum);
                areaResults.Add(check);

                if (check == ScanResult.CheckSumError)
                {
                    return ScanResult.CheckSumError;
                }

                if (check > lastResult)
                {
                    lastResult = check;
                }

                if (check == ScanResult.Ok)
                {
                    code.AddRange(data);
                    lastCheckSum = newCheckSum;
                    currentRow++;
                }
            }

            programCode = code;
            return lastResult;
        }

        /////////////////////////////////////////////////////////////

        private ScanResult DecodeArea(byte[,] blacks, Rectangle area, int lastCheckSum, int currentRow, out byte[] programData, out int newCheckSum)
        {
            ScanResult lastResult = ScanResult.NoBarcode;

            void SetLastResult(ScanResult newResult)
            {
                if (lastResult < newResult)
                {
                    lastResult = newResult;
                }
            }

            programData = null;
            newCheckSum = 0;
            byte[] data;
            int y = (area.Bottom - area.Top) / 2 + area.Top;
            int testIndex = -1;
            int sign = -1;

            while (y >= area.Top && y <= area.Bottom)
            {
                testIndex += 2;
                sign *= -1;
                y += testIndex * sign;

                // first pass: try to decode normal
                var checkResult = CheckDecodeBarcodeRow(blacks, y, area.Left, area.Right, false, lastCheckSum, out newCheckSum, out data);
                SetLastResult(checkResult);

                if (checkResult == ScanResult.NoProgramCode)
                {
                    return ScanResult.NoProgramCode;
                }

                // in case of an error second pass: try to decode with wider black bars
                if (checkResult != ScanResult.Ok)
                {
                    checkResult = CheckDecodeBarcodeRow(blacks, y, area.Left, area.Right, true, lastCheckSum, out newCheckSum, out data);
                    SetLastResult(checkResult);

                    if (checkResult == ScanResult.NoProgramCode)
                    {
                        return ScanResult.NoProgramCode;
                    }

                    if (checkResult != ScanResult.Ok)
                    {
                        continue;
                    }
                }

                var check = IsProgramBarcode(data, currentRow);

                if (!check)
                {
                    return ScanResult.NoProgramCode;
                }

                programData = new byte[data.Length - ProgramBarcodeHeaderLength];
                Array.Copy(data, ProgramBarcodeHeaderLength, programData, 0, data.Length - ProgramBarcodeHeaderLength);

                return ScanResult.Ok;
            }

            return lastResult;
        }

        /////////////////////////////////////////////////////////////

        ScanResult CheckDecodeBarcodeRow(byte[,] blacks, int row, int leftX, int rightX, bool checkWider, int lastCheckSum, out int newCheckSum, out byte[] data)
        {
            var check = DecodeBarcodeRow(blacks, row, leftX, rightX, checkWider, out data);

            if (!check || data.Count() < MinProgramBarcodeLength)
            {
                newCheckSum = 0;
                return ScanResult.NoBarcode;
            }

            check = CheckSum(data, lastCheckSum, out newCheckSum);

            if (!check)
            {
                if (data.Length < 10)
                {
                    check = CheckSum(data, 0, out int _);

                    if (check)
                    {
                        // we found a direct execution barcode, ignore it
                        return ScanResult.NoProgramCode;
                    }
                }

                return ScanResult.CheckSumError;
            }

            return ScanResult.Ok;
        }

        /////////////////////////////////////////////////////////////

        private bool DecodeBarcodeRow(byte[,] blacks, int row, int leftX, int rightX, bool checkWider, out byte[] data)
        {
            data = null;

            int firstBlackX = leftX;

            while (firstBlackX < rightX)
            {
                if (blacks[firstBlackX, row] == 0)
                {
                    break;
                }

                firstBlackX++;
            }

            if (firstBlackX == rightX)
            {
                return false;
            }

            int lastBlackX = rightX;

            while (lastBlackX > firstBlackX)
            {
                if (blacks[lastBlackX, row] == 0)
                {
                    break;
                }

                lastBlackX--;
            }

            // left and right black pixel shouldn't be too far away from the box's bounds
            if (firstBlackX == lastBlackX || firstBlackX > leftX * 1.3 || lastBlackX < rightX * 0.7)
            {
                return false;
            }

            var blackbars = new List<int>();
            var blackBarsStatistics = new List<int>();
            var whiteBarsStatistics = new List<int>();
            bool isBlack = true;
            int pixels = 0;

            for (int x = firstBlackX; x <= lastBlackX; x++)
            {
                if (isBlack)
                {
                    if (blacks[x, row] == 0)
                    {
                        pixels++;
                    }
                    else
                    {
                        blackbars.Add(pixels);

                        if (!blackBarsStatistics.Contains(pixels))
                        {
                            blackBarsStatistics.Add(pixels);
                        }

                        pixels = 1;
                        isBlack = false;
                    }
                }
                else
                {
                    if (blacks[x, row] != 0)
                    {
                        pixels++;
                    }
                    else
                    {
                        if (!whiteBarsStatistics.Contains(pixels))
                        {
                            whiteBarsStatistics.Add(pixels);
                        }

                        pixels = 1;
                        isBlack = true;
                    }
                }
            }

            blackbars.Add(pixels);

            // minimum number of bars required:
            // 2 bars start signature
            // 2 bars end signature
            // header
            // is always full bytes
            if (blackbars.Count < 4 + MinProgramBarcodeLength * 8 || ((blackbars.Count - 4) % 8) != 0)
            {
                return false;
            }

            int minNarrowBarWidth = whiteBarsStatistics.Min();
            int maxNarrowBarWidth = whiteBarsStatistics.Max();

            if (maxNarrowBarWidth > 3 * minNarrowBarWidth)
            {
                return false;
            }

            int minWideBarWidth;
            int maxWidewBarWidth;

            blackBarsStatistics.Sort();
            var len = blackBarsStatistics.Count;

            if (len >= 2)
            {
                maxWidewBarWidth = blackBarsStatistics[len - 1];

                if (len % 2 == 0)
                {
                    minWideBarWidth = blackBarsStatistics[len / 2];
                }
                else
                {
                    if (checkWider)
                    {
                        minWideBarWidth = blackBarsStatistics[len / 2 + 1];
                    }
                    else
                    {
                        minWideBarWidth = blackBarsStatistics[len / 2];
                    }
                }
            }
            else
            {
                minWideBarWidth = blackBarsStatistics[0];
                maxWidewBarWidth = minWideBarWidth;
            }


            // add some senity checks about black and white bar widths here

            int numBars = blackbars.Count;
            bool[] bools = new bool[numBars];

            for (int i = 0; i < numBars; i++)
            {
                bools[i] = blackbars[i] >= minWideBarWidth;
            }

            // check start and end signature
            if (!(!bools[0] && !bools[1] && bools[numBars - 2] && !bools[numBars - 1]))
            {
                return false;
            }

            byte[] bytes = new byte[(numBars - 4) / 8];
            int byteIndex = 0;

            for (int i = 2; i < numBars - 4; i += 8)
            {
                byte b = 0;

                for (int j = 0; j < 8; j++)
                {
                    b = (byte)((b << 1) | (bools[i + j] ? (byte)1 : (byte)0));
                }

                bytes[byteIndex++] = b;
            }

            data = bytes;

            return true;
        }

        /////////////////////////////////////////////////////////////

        private bool IsProgramBarcode(byte[] barcode, int currentRow)
        {
            if (barcode.Length < MinProgramBarcodeLength)
            {
                return false;
            }

            if ((barcode[1] >> 4) != 1)
            {
                return false;
            }

            if ((barcode[1] & 0x0F) != (currentRow % 16))
            {
                return false;
            }

            return true;
        }

        /////////////////////////////////////////////////////////////

        private bool CheckSum(byte[] bytes, int lastCheckSum, out int newcheckSum)
        {
            int check = lastCheckSum;

            for (int i = 1; i < bytes.Length; i++)
            {
                check += bytes[i];

                if (check > 0xFF)
                {
                    check -= 0xFF;
                }
            }

            newcheckSum = check;

            return check == bytes[0];
        }

        /////////////////////////////////////////////////////////////

        private byte[,] Binarize(byte[,] grayImage)
        {
            int width = grayImage.GetLength(0);
            int height = grayImage.GetLength(1);
            byte[,] bin = new byte[width, height];
            byte min = byte.MaxValue;
            byte max = byte.MinValue;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var val = grayImage[x, y];

                    if (val < min)
                    {
                        min = val;
                    }

                    if (val > max)
                    {
                        max = val;
                    }
                }
            }

            double f = 0.5;
            byte threshold = (byte)((max - min) * f);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var val = grayImage[x, y];

                    bin[x, y] = val < threshold ? (byte)0 : (byte)255;
                }
            }

            return bin;
        }

        /////////////////////////////////////////////////////////////

        private unsafe byte[,] CreateGrayScale(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            byte* p = (byte*)(void*)bitmapData.Scan0;
            int stride = bitmapData.Stride;    // stride is offset between horizontal lines in p 
            byte[,] grays = new byte[bitmap.Width, bitmap.Height];

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    grays[x, y] = GetPixelBrightness(p, bitmap.PixelFormat, stride, x, y);
                }
            }

            bitmap.UnlockBits(bitmapData);

            return grays;
        }

        /////////////////////////////////////////////////////////////

        private unsafe byte GetPixelBrightness(byte* pixelArray, System.Drawing.Imaging.PixelFormat pixelFormat, int stride, int x, int y)
        {
            byte brightness = 0;

            switch (pixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format1bppIndexed:
                {
                    byte pixel = pixelArray[(y * stride) + (x >> 3)];

                    if (((pixel << (x % 8)) & 128) != 0)
                    {
                        brightness = 255;
                    }

                    break;
                }

                default: // 24bpp RGB, 32bpp formats
                {
                    int pixelIndex = (y * stride) + (x * (pixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb ? 3 : 4));
                    ushort sum = 0;

                    for (int i = pixelIndex; i < pixelIndex + 3; i++)
                    {
                        sum += pixelArray[i];
                    }

                    brightness = (byte)(sum / 3);
                    break;
                }
            }

            return brightness;
        }
    }
}
