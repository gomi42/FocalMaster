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
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using FocalDecompiler;

namespace FocalCompiler
{
    /////////////////////////////////////////////////////////////

    internal enum EdgeOrientation : byte
    {
        None,
        Vertical,
        Horizontal
    }
    
    /////////////////////////////////////////////////////////////

    internal enum ScanResult
    {
        NoBarcode = 0,
        NoProgramCode = 1,
        CheckSumError = 2,
        ProgramCode = 3
    }

    /////////////////////////////////////////////////////////////

    internal class ErrorImageData
    {
        public string Filename;
        public byte[,] GrayImage;
        public EdgeOrientation[,] Edges;
        public List<Rectangle> BarcodeAreas;
        public List<ScanResult> AreaResults;
    }

    /////////////////////////////////////////////////////////////

    internal class BarcodeScanner
    {
        private const int ProgramBarcodeHeaderLength = 3;
        private const int MinProgramBarcodeLength = ProgramBarcodeHeaderLength + 1;

        private const int MinBoxSize = 6;
        private const int MaxBoxSize = 20;

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
                var edges = GetEdges(binaryImage);

                var boxSize = MinBoxSize;
                ScanResult scanResult;
                List<Rectangle> barcodeAreas;
                List<ScanResult> areaResults;
                List<byte> code;

                do
                {
                    var boxes = FindBoxes(edges, boxSize);
                    barcodeAreas = CombineBoxesToAreas(boxes, boxSize);

                    scanResult = DecodeBarcodes(binaryImage, barcodeAreas, ref lastCheckSum, ref currentRow, out code, out areaResults);

                    boxSize += 1;
                }
                while (boxSize < MaxBoxSize && scanResult != ScanResult.ProgramCode);

                if (scanResult != ScanResult.ProgramCode)
                {
                    switch (scanResult)
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
                bitmap.Dispose();
            }

            var decomp = new Decompiler();
            decomp.Decompile(programCode, out string focal);

            ErrorImageData = null;

            return focal;
        }

        /////////////////////////////////////////////////////////////

        public List<ErrorImageData> ScanDebug(List<string> files)
        {
            Errors = new List<string>();
            int lastCheckSum = 0;
            int currentRow = 0;
            var results = new List<ErrorImageData>();

            foreach (var file in files)
            {
                var bitmap = (Bitmap)Image.FromFile(file);
                var grayImage = CreateGrayScale(bitmap);
                var binaryImage = Binarize(grayImage);
                var edges = GetEdges(binaryImage);

                var boxSize = MinBoxSize;
                ScanResult scanResult;
                List<Rectangle> barcodeAreas;
                List<ScanResult> areaResults;

                do
                {
                    var boxes = FindBoxes(edges, boxSize);
                    barcodeAreas = CombineBoxesToAreas(boxes, boxSize);

                    scanResult = DecodeBarcodes(binaryImage, barcodeAreas, ref lastCheckSum, ref currentRow, out _, out areaResults);

                    boxSize += 1;
                }
                while (boxSize <= MaxBoxSize && scanResult != ScanResult.ProgramCode);

                var imageData = new ErrorImageData { Filename = file, GrayImage = grayImage, BarcodeAreas = barcodeAreas, AreaResults = areaResults };
                results.Add(imageData);

                if (scanResult != ScanResult.ProgramCode)
                {
                    switch (scanResult)
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
                }

                bitmap.Dispose();
            }

            return results;
        }

        /////////////////////////////////////////////////////////////

        public List<ErrorImageData> ScanDebugBoxes(List<string> files)
        {
            Errors = new List<string>();
            var results = new List<ErrorImageData>();

            var file = files[0];

            var bitmap = (Bitmap)Image.FromFile(file);
            var grayImage = CreateGrayScale(bitmap);
            var binaryImage = Binarize(grayImage);
            var edges = GetEdges(binaryImage);

            var boxSize = MinBoxSize;
            var boxes = FindBoxes(edges, boxSize);
            var areas = CombineBoxesToAreas(boxes, boxSize);

            var imageData = new ErrorImageData { Filename = file, GrayImage = binaryImage, BarcodeAreas = areas, AreaResults = null };
            results.Add(imageData);

            areas = CreateBoxAreas(boxes, boxSize);
            imageData = new ErrorImageData { Filename = file, GrayImage = binaryImage, BarcodeAreas = areas, AreaResults = null };
            results.Add(imageData);

            imageData = new ErrorImageData { Filename = file, Edges = edges, BarcodeAreas = null, AreaResults = null };
            results.Add(imageData);

            bitmap.Dispose();

            return results;
        }

        /////////////////////////////////////////////////////////////

        private EdgeOrientation[,] GetEdges(byte[,] blacks)
        {
            const int EdgeThreshold = 100 * 3;

            var width = blacks.GetLength(0);
            var height = blacks.GetLength(1);
            EdgeOrientation[,] edges = new EdgeOrientation[width, height];

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    int edgeX = blacks[x - 1, y - 1] - blacks[x, y - 1] +
                                blacks[x - 1, y] - blacks[x, y] +
                                blacks[x - 1, y + 1] - blacks[x, y + 1];
                    int edgeY = blacks[x - 1, y - 1] + blacks[x, y - 1] + blacks[x + 1, y - 1] -
                                blacks[x - 1, y] - blacks[x, y] - blacks[x + 1, y];

                    edgeX = Math.Abs(edgeX);
                    edgeY = Math.Abs(edgeY);

                    if (edgeX > EdgeThreshold)
                    {
                        edges[x, y] = EdgeOrientation.Vertical;
                    }
                    else
                    if (edgeY > EdgeThreshold)
                    {
                        edges[x, y] = EdgeOrientation.Horizontal;
                    }
                    else
                    {
                        edges[x, y] = EdgeOrientation.None;
                    }
                }
            }

            return edges;
        }

        /////////////////////////////////////////////////////////////

        private bool[,] FindBoxes(EdgeOrientation[,] edges, int boxSize)
        {
            const int VerticalHorizontalRatio = 3;

            var width = edges.GetLength(0);
            var height = edges.GetLength(1);
            int xBoxes = width / boxSize;
            int yBoxes = height / boxSize;
            var boxes = new bool[xBoxes, yBoxes];

            for (int boxY = 0; boxY < yBoxes; boxY++)
            {
                int boxPixelStartY = boxY * boxSize;

                for (int boxX = 0; boxX < xBoxes; boxX++)
                {
                    int boxPixelStartX = boxX * boxSize;
                    int sumX = 0;
                    int sumY = 0;

                    for (int yBoxPixel = boxPixelStartY; yBoxPixel < boxPixelStartY + boxSize; yBoxPixel++)
                    {
                        for (int xBoxPixel = boxPixelStartX; xBoxPixel < boxPixelStartX + boxSize; xBoxPixel++)
                        {
                            if (edges[xBoxPixel, yBoxPixel] == EdgeOrientation.Vertical)
                            {
                                sumX++;
                            }
                            else
                            if (edges[xBoxPixel, yBoxPixel] == EdgeOrientation.Horizontal)
                            {
                                sumY++;
                            }
                        }
                    }

                    if (sumX >= boxSize && sumX > sumY * VerticalHorizontalRatio)
                    {
                        boxes[boxX, boxY] = true;
                    }
                }
            }

            return boxes;
        }

        /////////////////////////////////////////////////////////////

        private List<Rectangle> CombineBoxesToAreas(bool[,] boxes, int boxSize)
        {
            const int MinBoxesHeight = 3;
            const int MinBoxesWidth = 5;

            var areas = new List<Rectangle>();
            var width = boxes.GetLength(0);
            var height = boxes.GetLength(1);
            bool[,] visited = new bool[width, height];

            int minX;
            int maxX;
            int minY;
            int maxY;

            ///////////////////////////////////

            bool FindNextXY(int startFindX, int startFindY, int yToStartXAt0, out int foundX, out int foundY)
            {
                foundX = -1;
                foundY = -1;

                while (startFindY < height)
                {
                    foundX = -1;

                    for (int x = startFindX; x < width; x++)
                    {
                        if (boxes[x, startFindY] && !visited[x, startFindY])
                        {
                            foundX = x;
                            break;
                        }
                    }

                    if (foundX > 0)
                    {
                        foundY = startFindY;
                        return true;
                    }

                    startFindY++;

                    if (startFindY > yToStartXAt0)
                    {
                        startFindX = 0;
                    }
                }

                return false;
            }

            ///////////////////////////////////

            void DetermineArea(int areaX, int areaY)
            {
                if (areaX < 0 || areaX >= width || areaY < 0 || areaY >= height)
                {
                    return;
                }

                if (visited[areaX, areaY])
                {
                    return;
                }

                visited[areaX, areaY] = true;

                if (!boxes[areaX, areaY])
                {
                    return;
                }

                if (areaX < minX)
                    minX = areaX;

                if (areaX > maxX)
                    maxX = areaX;

                if (areaY < minY)
                    minY = areaY;

                if (areaY > maxY)
                    maxY = areaY;

                DetermineArea(areaX - 1, areaY);
                DetermineArea(areaX + 1, areaY);
                DetermineArea(areaX, areaY + 1);
                DetermineArea(areaX, areaY - 1);

                return;
            }

            ///////////////////////////////////

            bool IsNumberOfBoxesInRange(int testY, int minAreaWidth)
            {
                int count = 0;
                    
                for (int x = minX; x <= maxX; x++)
                {
                    if (boxes[x, testY])
                    {
                        count++;
                    }
                }

                if (count < minAreaWidth)
                {
                    return false;
                }

                return true;
            }

            ///////////////////////////////////

            void ShrinkArea()
            {
                var minBoxesInRow = ((maxX - minX + 1) * 8) / 10;

                while (!IsNumberOfBoxesInRange(minY, minBoxesInRow) && minY <= maxY - 2)
                {
                    minY++;
                }

                while (!IsNumberOfBoxesInRange(maxY, minBoxesInRow) && maxY >= minY + 2)
                {
                    maxY--;
                }
            }

            ///////////////////////////////////

            void SplitArea()
            {
                var minBoxesInRow = (maxX - minX + 1) / 7;
                int areaStartY = minY;
                int areaEndY;

                while (areaStartY < maxY)
                {
                    areaEndY = areaStartY;

                    do
                    {
                        areaEndY++;
                    }
                    while (areaEndY <= maxY && IsNumberOfBoxesInRange(areaEndY, minBoxesInRow));

                    areaEndY--;

                    if ((areaEndY - areaStartY + 1) >= MinBoxesHeight)
                    {
                        var re = new Rectangle(minX * boxSize, areaStartY * boxSize, (maxX - minX) * boxSize + boxSize, (areaEndY - areaStartY) * boxSize + boxSize);
                        areas.Add(re);
                    }

                    do
                    {
                        areaEndY++;
                    }
                    while (areaEndY <= maxY && !IsNumberOfBoxesInRange(areaEndY, minBoxesInRow));

                    areaStartY = areaEndY;
                }
            }

            ///////////////////////////////////

            int y = 0;
            int lastMaxX = 0;
            int lastMaxY = 0;

            while (y < height)
            {
                if (!FindNextXY(lastMaxX, y, lastMaxY, out int foundX, out int foundY))
                {
                    y++;
                    continue;
                }

                minX = int.MaxValue;
                maxX = int.MinValue;
                minY = int.MaxValue;
                maxY = int.MinValue;

                DetermineArea(foundX, foundY);

                if (minX < int.MaxValue)
                {
                    if ((maxX - minX + 1) >= MinBoxesWidth && (maxY - minY + 1) >= MinBoxesHeight)
                    {
                        ShrinkArea();

                        if ((maxY - minY + 1) >= MinBoxesHeight)
                        {
                            SplitArea();
                        }
                    }

                    y = foundY;
                    lastMaxX = maxX;
                    lastMaxY = maxY;
                }
                else
                {
                    y++;
                    lastMaxX = 0;
                    lastMaxY = 0;
                }
            }

            return areas;
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
                var scanResult = DecodeArea(blacks, area, lastCheckSum, currentRow, out data, out int newCheckSum);
                areaResults.Add(scanResult);

                if (scanResult == ScanResult.CheckSumError)
                {
                    return ScanResult.CheckSumError;
                }

                if (scanResult > lastResult)
                {
                    lastResult = scanResult;
                }

                if (scanResult == ScanResult.ProgramCode)
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
                testIndex += 1;
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
                if (checkResult != ScanResult.ProgramCode)
                {
                    checkResult = CheckDecodeBarcodeRow(blacks, y, area.Left, area.Right, true, lastCheckSum, out newCheckSum, out data);
                    SetLastResult(checkResult);

                    if (checkResult == ScanResult.NoProgramCode)
                    {
                        return ScanResult.NoProgramCode;
                    }

                    if (checkResult != ScanResult.ProgramCode)
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

                return ScanResult.ProgramCode;
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

            return ScanResult.ProgramCode;
        }

        /////////////////////////////////////////////////////////////

        private bool DecodeBarcodeRow(byte[,] blacks, int row, int leftX, int rightX, bool checkWider, out byte[] data)
        {
            data = null;

            int firstBlackX = leftX;

            while (firstBlackX < rightX)
            {
                if (blacks[firstBlackX, row] != 0)
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
                if (blacks[lastBlackX, row] != 0)
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
                    if (blacks[x, row] != 0)
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
                    if (blacks[x, row] == 0)
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
            int checkSum = lastCheckSum;

            for (int i = 1; i < bytes.Length; i++)
            {
                checkSum += bytes[i];

                if (checkSum > 0xFF)
                {
                    checkSum -= 0xFF;
                }
            }

            newcheckSum = checkSum;

            return checkSum == bytes[0];
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

            byte threshold = (byte)((max - min) * 0.5);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var val = grayImage[x, y];

                    bin[x, y] = val < threshold ? (byte)255 : (byte)0;
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

        private unsafe byte GetPixelBrightness(byte* pixelArray, PixelFormat pixelFormat, int stride, int x, int y)
        {
            byte brightness = 0;

            switch (pixelFormat)
            {
                case PixelFormat.Format1bppIndexed:
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
                    int pixelIndex = (y * stride) + (x * (pixelFormat == PixelFormat.Format24bppRgb ? 3 : 4));
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

        /////////////////////////////////////////////////////////////

        private List<Rectangle> CreateBoxAreas(bool[,] boxes, int boxSize)
        {
            var results = new List<Rectangle>();
            var width = boxes.GetLength(0);
            var height = boxes.GetLength(1);

            for (int yBox = 0; yBox < height; yBox++)
            {
                int y = yBox * boxSize;

                for (int xBox = 0; xBox < width; xBox++)
                {
                    if (boxes[xBox, yBox])
                    {
                        int x = xBox * boxSize;
                        var re = new Rectangle(x, y, boxSize - 1, boxSize - 1);
                        results.Add(re);
                    }
                }
            }

            return results;
        }
    }
}
