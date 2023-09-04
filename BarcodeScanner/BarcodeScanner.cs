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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ShapeConverter.Parser.Pdf;

namespace FocalMaster
{
    /////////////////////////////////////////////////////////////

    internal class BarcodeScanner
    {
        enum DecodeBarcodeRowResult
        {
            Ok,
            EmptyRow,
            InvalidRowWithinBounds,
            InvalidNumberOfBars,
            InvalidNarrowBarWidthDistribution,
            InvalidSignature
        }

        /////////////////////////////////////////////////////////////

        abstract class BitmapInfo
        {
            public BitmapInfo(string filename)
            {
                Filename = filename;
            }

            public string Filename { get; }
        }

        /////////////////////////////////////////////////////////////

        class BitmapInfoError : BitmapInfo
        {
            public BitmapInfoError(string filename)
                    : base(filename)
            {
            }
        }

        /////////////////////////////////////////////////////////////

        abstract class BitmapInfoBitmap : BitmapInfo
        {
            public BitmapInfoBitmap(Bitmap bitmap, string filename)
                    : base(filename)
            {
                Bitmap = bitmap;
            }

            public Bitmap Bitmap { get; }
        }

        /////////////////////////////////////////////////////////////

        class BitmapInfoImage : BitmapInfoBitmap
        {
            public BitmapInfoImage(Bitmap bitmap, string filename)
                    : base(bitmap, filename)
            {
            }
        }

        /////////////////////////////////////////////////////////////

        abstract class BitmapInfoPage : BitmapInfoBitmap
        {
            public BitmapInfoPage(Bitmap bitmap, string filename, int pageNumber)
                    : base(bitmap, filename)
            {
                PageNumber = pageNumber;
            }

            public int PageNumber { get; }
        }

        /////////////////////////////////////////////////////////////

        class BitmapInfoPageImage : BitmapInfoPage
        {
            public BitmapInfoPageImage(Bitmap bitmap, string filename, int pageNumber, int imageNumber)
                    : base(bitmap, filename, pageNumber)
            {
                ImageNumber = imageNumber;
            }

            public int ImageNumber { get; }
        }

        /////////////////////////////////////////////////////////////

        class BitmapInfoPageGraphic : BitmapInfoPage
        {
            public BitmapInfoPageGraphic(Bitmap bitmap, string filename, int pageNumber)
                    : base(bitmap, filename, pageNumber)
            {
            }
        }

        /////////////////////////////////////////////////////////////

        private const int ProgramBarcodeHeaderLength = 3;
        private const int MinProgramBarcodeLength = ProgramBarcodeHeaderLength + 1;

        private const int MinBoxSize = 4;
        private const int MaxBoxSize = 20;

        private int numParallelThreads;

        /////////////////////////////////////////////////////////////

        public BarcodeScanner()
        {
            numParallelThreads = Environment.ProcessorCount - 3;

            if (numParallelThreads <= 0)
            {
                numParallelThreads = 1;
            }
        }

        /////////////////////////////////////////////////////////////

        public TimeSpan LastDuration { get; private set; }

        /////////////////////////////////////////////////////////////

        public bool Scan(List<string> files, out byte[] code, out List<ScannerResult> scannerResults)
        {
            scannerResults = new List<ScannerResult>();
            var programCode = new List<byte>();
            int lastCheckSum = 0;
            int currentRow = 0;
            DateTime time = DateTime.Now;

            var bitmapInfos = GetBitmaps(files);

            foreach (var bitmapInfo in bitmapInfos)
            {
                if (bitmapInfo is BitmapInfoError)
                {
                    scannerResults.Add(new ScannerResultError(ScannerResultId.CannotOpenFile, bitmapInfo.Filename));
                    code = null;
                    return false;
                }

                BitmapInfoBitmap bitmapInfoBitmap = (BitmapInfoBitmap)bitmapInfo;
                ScanResult fileScanResult = ScanOneBitmap(bitmapInfoBitmap, ref lastCheckSum, ref currentRow, out List<byte> bitmapCode, out _);
                scannerResults.Add(GetScannerResult(fileScanResult, bitmapInfoBitmap));
                bitmapInfoBitmap.Bitmap.Dispose();

                switch (fileScanResult)
                {
                    case ScanResult.CheckSumError:
                        code = null;
                        return false;

                    case ScanResult.ProgramCode:
                        programCode.AddRange(bitmapCode);
                        break;
                }
            }

            code = programCode.ToArray();
            LastDuration = DateTime.Now - time;

            return true;
        }

        /////////////////////////////////////////////////////////////

        public void ScanDebug(List<string> files, out List<ImageResult> imageResults, out List<ScannerResult> scannerResults)
        {
            imageResults = new List<ImageResult>();
            scannerResults = new List<ScannerResult>();
            int lastCheckSum = 0;
            int currentRow = 0;
            DateTime time = DateTime.Now;

            var bitmapInfos = GetBitmaps(files);

            foreach (var bitmapInfo in bitmapInfos)
            {
                if (bitmapInfo is BitmapInfoError)
                {
                    scannerResults.Add(new ScannerResultError(ScannerResultId.CannotOpenFile, bitmapInfo.Filename));
                    continue;
                }

                BitmapInfoBitmap bitmapInfoBitmap = (BitmapInfoBitmap)bitmapInfo;
                ScanResult fileScanResult = ScanOneBitmap(bitmapInfoBitmap, ref lastCheckSum, ref currentRow, out _, out ImageResult imageResult);
                scannerResults.Add(GetScannerResult(fileScanResult, bitmapInfoBitmap));
                imageResults.Add(imageResult);
                bitmapInfoBitmap.Bitmap.Dispose();
            }

            LastDuration = DateTime.Now - time;
        }

        /////////////////////////////////////////////////////////////

        private ScanResult ScanOneBitmap(BitmapInfoBitmap bitmapInfoBitmap, ref int lastCheckSum, ref int currentRow, out List<byte> code, out ImageResult imageResult)
        {
            var grayImage = CreateGrayScale(bitmapInfoBitmap.Bitmap);
            var binaryImage = Binarize(grayImage);
            var edges = GetEdges(binaryImage);

            var boxSize = MinBoxSize;
            ScanResult scanResult;
            ScanResult fileScanResult = ScanResult.NoBarcodeFound;
            List<Rectangle> barcodeAreas;
            List<ScanResult> areaResults;

            do
            {
                var boxes = FindBoxes(edges, boxSize);
                barcodeAreas = CombineBoxesToAreas(boxes, boxSize);

                scanResult = DecodeBarcodes(binaryImage, barcodeAreas, ref lastCheckSum, ref currentRow, out code, out areaResults);

                if (scanResult > fileScanResult)
                {
                    fileScanResult = scanResult;
                }

                boxSize += 1;
            }
            while (boxSize <= MaxBoxSize && fileScanResult != ScanResult.ProgramCode);

            imageResult = new ImageResult(bitmapInfoBitmap.Filename, grayImage, barcodeAreas, areaResults);

            return fileScanResult;
        }

        /////////////////////////////////////////////////////////////

#if DEBUG
        public void ScanDebugBoxes(List<string> files, out List<ImageResult> imageResults)
        {
            imageResults = new List<ImageResult>();
            DateTime time = DateTime.Now;

            var file = files[0];

            var bitmaps = GetBitmaps(files);
            var bitmapEnumerator = bitmaps.GetEnumerator();
            bitmapEnumerator.MoveNext();
            BitmapInfo bitmapInfo = bitmapEnumerator.Current;
            BitmapInfoBitmap bitmapInfoBitmap = (BitmapInfoBitmap)bitmapInfo;

            var grayImage = CreateGrayScale(bitmapInfoBitmap.Bitmap);
            var binaryImage = Binarize(grayImage);
            var edges = GetEdges(binaryImage);

            var boxSize = MinBoxSize;
            var boxes = FindBoxes(edges, boxSize);
            var areas = CombineBoxesToAreas(boxes, boxSize);

            var imageData = new ImageResult(file, binaryImage);
            imageResults.Add(imageData);

            imageData = new ImageResult(file, binaryImage, areas);
            imageResults.Add(imageData);

            var boxAreas = CreateBoxAreas(boxes, boxSize);
            imageData = new ImageResult(file, binaryImage, boxAreas);
            imageResults.Add(imageData);

            imageData = new ImageResult(file, edges);
            imageResults.Add(imageData);

            bitmapInfoBitmap.Bitmap.Dispose();
            LastDuration = DateTime.Now - time;
        }
#endif

        /////////////////////////////////////////////////////////////

        private IEnumerable<BitmapInfo> GetBitmaps(List<string> files)
        {
            foreach (var file in files)
            {
                string filename = Path.GetFileName(file);

                if (Path.GetExtension(file).ToLower() == ".pdf")
                {
                    IEnumerable<PdfBitmapInfo> pdfBitmaps = null;

                    try
                    {
                        var pdfParser = new PdfParser();
                        pdfBitmaps = pdfParser.Parse(file);
                    }
                    catch
                    {
                    }

                    if (pdfBitmaps != null)
                    {
                        foreach (var pdfBitmapInfo in pdfBitmaps)
                        {
                            switch (pdfBitmapInfo)
                            {
                                case PdfBitmapInfoImage image:
                                    yield return new BitmapInfoPageImage(image.Bitmap, filename, image.PageNumber, image.ImageNumber);
                                    break;

                                case PdfBitmapInfoGraphic graphic:
                                    yield return new BitmapInfoPageGraphic(graphic.Bitmap, filename, graphic.PageNumber);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        yield return new BitmapInfoError(filename);
                    }
                }
                else
                {
                    Bitmap imageBitmap = null;

                    try
                    {
                        imageBitmap = (Bitmap)Image.FromFile(file);
                    }
                    catch
                    {
                    }

                    if (imageBitmap != null)
                    {
                        yield return new BitmapInfoImage(imageBitmap, filename);
                    }
                    else
                    {
                        yield return new BitmapInfoError(filename);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////

        private ScannerResult GetScannerResult(ScanResult scanResult, BitmapInfo bitmapInfo)
        {
            ScannerResultId scanerResultId;
            ScannerResult result;

            switch (scanResult)
            {
                case ScanResult.NoBarcodeFound:
                    scanerResultId = ScannerResultId.NoBarcodeFound;
                    break;

                case ScanResult.NoProgramCode:
                    scanerResultId = ScannerResultId.NoProgramCode;
                    break;

                case ScanResult.InvalidSignature:
                    scanerResultId = ScannerResultId.InvalidSignature;
                    break;

                case ScanResult.CheckSumError:
                    scanerResultId = ScannerResultId.CheckSumError;
                    break;

                case ScanResult.ProgramCode:
                    scanerResultId = ScannerResultId.ProgramCode;
                    break;

                default:
                    throw new ArgumentException("Unknown scan result");
            }

            switch (bitmapInfo)
            {
                case BitmapInfoPageGraphic graphic:
                    result = new ScannerResultPageGraphic(scanerResultId, graphic.Filename, graphic.PageNumber);
                    break;

                case BitmapInfoPageImage image:
                    result = new ScannerResultPageImage(scanerResultId, image.Filename, image.PageNumber, image.ImageNumber);
                    break;

                case BitmapInfoImage image:
                    result = new ScannerResultImage(scanerResultId, image.Filename);
                    break;

                default:
                    throw new ArgumentException("Unknown scan result");
            }

            return result;
        }

        /////////////////////////////////////////////////////////////

        private EdgeOrientation[,] GetEdges(byte[,] blacks)
        {
            var width = blacks.GetLength(0);
            var height = blacks.GetLength(1);
            EdgeOrientation[,] edges = new EdgeOrientation[width, height];

            if (numParallelThreads > 1)
            {
                Task[] tasks = new Task[numParallelThreads];

                int step = (height - 2) / numParallelThreads;
                int fromY = 1;
                int s;

                for (s = 0; s < numParallelThreads - 1; s++)
                {
                    tasks[s] = Task.Factory.StartNew((y) => GetEdgesSlice(blacks, width, (int)y, (int)y + step, edges), fromY);
                    fromY += step;
                }

                tasks[s] = Task.Factory.StartNew((y) => GetEdgesSlice(blacks, width, (int)y, height - 1, edges), fromY);

                Task.WaitAll(tasks);
            }
            else
            {
                GetEdgesSlice(blacks, width, 1, height - 1, edges);
            }

            return edges;
        }

        /////////////////////////////////////////////////////////////

        private void GetEdgesSlice(byte[,] blacks, int width, int yBoxFrom, int yBoxTo, EdgeOrientation[,] edges)
        {
            const int EdgeThreshold = 100 * 3;

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = yBoxFrom; y < yBoxTo; y++)
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
        }

        /////////////////////////////////////////////////////////////

        private bool[,] FindBoxes(EdgeOrientation[,] edges, int boxSize)
        {
            var width = edges.GetLength(0);
            var height = edges.GetLength(1);
            int xBoxes = width / boxSize;
            int yBoxes = height / boxSize;
            var boxes = new bool[xBoxes, yBoxes];

            if (numParallelThreads > 1)
            {
                Task[] tasks = new Task[numParallelThreads];

                int step = yBoxes / numParallelThreads;
                int fromY = 0;
                int s;

                for (s = 0; s < numParallelThreads - 1; s++)
                {
                    tasks[s] = Task.Factory.StartNew((y) => FindBoxesSlice(edges, xBoxes, (int)y, (int)y + step, boxSize, boxes), fromY);
                    fromY += step;
                }

                tasks[s] = Task.Factory.StartNew((y) => FindBoxesSlice(edges, xBoxes, (int)y, yBoxes, boxSize, boxes), fromY);

                Task.WaitAll(tasks);
            }
            else
            {
                FindBoxesSlice(edges, xBoxes, 0, yBoxes, boxSize, boxes);
            }

            return boxes;
        }

        /////////////////////////////////////////////////////////////

        private void FindBoxesSlice(EdgeOrientation[,] edges, int xBoxes, int yBoxFrom, int yBoxTo, int boxSize, bool[,] boxes)
        {
            const int VerticalHorizontalRatio = 3;

            for (int boxY = yBoxFrom; boxY < yBoxTo; boxY++)
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
        }

        /////////////////////////////////////////////////////////////

        private List<Rectangle> CombineBoxesToAreas(bool[,] boxes, int boxSize)
        {
            const int MinBoxesHeight = 3;
            const int MaxBoxesHeight = 30;
            const int MinBoxesWidth = 5;

            var areas = new List<Rectangle>();
            var width = boxes.GetLength(0);
            var height = boxes.GetLength(1);
            bool[,] visited = new bool[width, height];

            int areaMinX;
            int areaMaxX;
            int areaMinY;
            int areaMaxY;

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

                if (areaX < areaMinX)
                    areaMinX = areaX;

                if (areaX > areaMaxX)
                    areaMaxX = areaX;

                if (areaY < areaMinY)
                    areaMinY = areaY;

                if (areaY > areaMaxY)
                    areaMaxY = areaY;

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

                for (int x = areaMinX; x <= areaMaxX; x++)
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
                var minBoxesInRow = ((areaMaxX - areaMinX + 1) * 8) / 10;

                while (!IsNumberOfBoxesInRange(areaMinY, minBoxesInRow) && areaMinY <= areaMaxY - 2)
                {
                    areaMinY++;
                }

                while (!IsNumberOfBoxesInRange(areaMaxY, minBoxesInRow) && areaMaxY >= areaMinY + 2)
                {
                    areaMaxY--;
                }
            }

            ///////////////////////////////////

            void SplitArea()
            {
                var minBoxesInRow = (areaMaxX - areaMinX + 1) / 7;
                int areaStartY = areaMinY;
                int areaEndY;

                while (areaStartY < areaMaxY)
                {
                    areaEndY = areaStartY;

                    do
                    {
                        areaEndY++;
                    }
                    while (areaEndY <= areaMaxY && IsNumberOfBoxesInRange(areaEndY, minBoxesInRow));

                    areaEndY--;

                    if ((areaEndY - areaStartY + 1) >= MinBoxesHeight)
                    {
                        var re = new Rectangle(areaMinX * boxSize, areaStartY * boxSize, (areaMaxX - areaMinX + 1) * boxSize, (areaEndY - areaStartY + 1) * boxSize);
                        areas.Add(re);
                    }

                    do
                    {
                        areaEndY++;
                    }
                    while (areaEndY <= areaMaxY && !IsNumberOfBoxesInRange(areaEndY, minBoxesInRow));

                    areaStartY = areaEndY;
                }
            }

            ///////////////////////////////////

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!boxes[x, y] || visited[x, y])
                    {
                        continue;
                    }

                    areaMinX = int.MaxValue;
                    areaMaxX = int.MinValue;
                    areaMinY = int.MaxValue;
                    areaMaxY = int.MinValue;

                    DetermineArea(x, y);

                    if (areaMinX < int.MaxValue)
                    {
                        if ((areaMaxX - areaMinX + 1) >= MinBoxesWidth && (areaMaxY - areaMinY + 1) >= MinBoxesHeight && (areaMaxY - areaMinY + 1) <= MaxBoxesHeight)
                        {
                            ShrinkArea();

                            if ((areaMaxY - areaMinY + 1) >= MinBoxesHeight)
                            {
                                SplitArea();
                            }
                        }
                    }
                }
            }

            return areas;
        }

        /////////////////////////////////////////////////////////////

        private ScanResult DecodeBarcodes(byte[,] blacks, List<Rectangle> barcodeAreas, ref int lastCheckSum, ref int currentRow, out List<byte> programCode, out List<ScanResult> areaResults)
        {
            programCode = null;
            areaResults = new List<ScanResult>();
            var code = new List<byte>();
            ScanResult lastResult = ScanResult.NoBarcodeFound;
            int rowChecksum = lastCheckSum;
            int row = currentRow;

            foreach (var area in barcodeAreas)
            {
                var scanResult = DecodeArea(blacks, area, rowChecksum, row, out byte[] data, out int checkSum);
                areaResults.Add(scanResult);

                if (scanResult == ScanResult.CheckSumError)
                {
                    return ScanResult.CheckSumError;
                }

                if (scanResult == ScanResult.InvalidSignature)
                {
                    return ScanResult.InvalidSignature;
                }

                if (scanResult > lastResult)
                {
                    lastResult = scanResult;
                }

                if (scanResult == ScanResult.ProgramCode)
                {
                    rowChecksum = checkSum;
                    code.AddRange(data);
                    row++;
                }
            }

            if (lastResult != ScanResult.ProgramCode)
            {
                return lastResult;
            }

            lastCheckSum = rowChecksum;
            currentRow = row;
            programCode = code;

            return lastResult;
        }

        /////////////////////////////////////////////////////////////

        private ScanResult DecodeArea(byte[,] blacks, Rectangle area, int lastCheckSum, int currentRow, out byte[] programData, out int newCheckSum)
        {
            ScanResult lastResult = ScanResult.NoBarcodeFound;

            void SetLastResult(ScanResult newResult)
            {
                if (lastResult < newResult)
                {
                    lastResult = newResult;
                }
            }

            programData = null;
            newCheckSum = 0;
            int checkSum;
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
                var checkResult = CheckDecodeBarcodeRow(blacks, y, area.Left, area.Right, false, lastCheckSum, out checkSum, out data);
                SetLastResult(checkResult);

                if (checkResult == ScanResult.NoProgramCode)
                {
                    return ScanResult.NoProgramCode;
                }

                // in case of an error second pass: try to decode with wider black bars
                if (checkResult != ScanResult.ProgramCode)
                {
                    checkResult = CheckDecodeBarcodeRow(blacks, y, area.Left, area.Right, true, lastCheckSum, out checkSum, out data);
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

                newCheckSum = checkSum;

                programData = new byte[data.Length - ProgramBarcodeHeaderLength];
                Array.Copy(data, ProgramBarcodeHeaderLength, programData, 0, data.Length - ProgramBarcodeHeaderLength);

                return ScanResult.ProgramCode;
            }

            return lastResult;
        }

        /////////////////////////////////////////////////////////////

        private ScanResult CheckDecodeBarcodeRow(byte[,] blacks, int row, int leftX, int rightX, bool checkWider, int lastCheckSum, out int newCheckSum, out byte[] data)
        {
            var result = DecodeBarcodeRow(blacks, row, leftX, rightX, checkWider, out data);

            if (result == DecodeBarcodeRowResult.InvalidSignature)
            {
                newCheckSum = 0;
                return ScanResult.InvalidSignature;
            }

            if (result != DecodeBarcodeRowResult.Ok || data.Count() < MinProgramBarcodeLength)
            {
                newCheckSum = 0;
                return ScanResult.NoBarcodeFound;
            }

            var check = CheckSum(data, lastCheckSum, out int checkSum);

            if (!check)
            {
                newCheckSum = 0;

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

            newCheckSum = checkSum;

            return ScanResult.ProgramCode;
        }

        /////////////////////////////////////////////////////////////

        private DecodeBarcodeRowResult DecodeBarcodeRow(byte[,] blacks, int row, int leftX, int rightX, bool checkWider, out byte[] data)
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
                return DecodeBarcodeRowResult.EmptyRow;
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

            if (firstBlackX == lastBlackX)
            {
                return DecodeBarcodeRowResult.InvalidRowWithinBounds;
            }

            // left and right black pixel shouldn't be too far away from the box's bounds
            if (firstBlackX > leftX * 1.3 || lastBlackX < rightX * 0.7)
            {
                return DecodeBarcodeRowResult.EmptyRow;
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
                return DecodeBarcodeRowResult.InvalidNumberOfBars;
            }

            int minNarrowBarWidth = whiteBarsStatistics.Min();
            int maxNarrowBarWidth = whiteBarsStatistics.Max();

            if (maxNarrowBarWidth > 3 * minNarrowBarWidth)
            {
                return DecodeBarcodeRowResult.InvalidNarrowBarWidthDistribution;
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
                return DecodeBarcodeRowResult.InvalidSignature;
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

            return DecodeBarcodeRowResult.Ok;
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

            if (numParallelThreads > 1)
            {
                Task[] tasks = new Task[numParallelThreads];
                int s;
                int step = height / numParallelThreads;
                int fromY = 0;
                (byte, byte)[] minMaxs = new (byte, byte)[numParallelThreads];

                for (s = 0; s < numParallelThreads - 1; s++)
                {
                    tasks[s] = Task.Factory.StartNew((t) => minMaxs[(((int, int))t).Item2] = GetMinMaxSlice(grayImage, width, (((int, int))t).Item1, (((int, int))t).Item1 + step), (fromY, s));
                    fromY += step;
                }

                tasks[s] = Task.Factory.StartNew((t) => minMaxs[(((int, int))t).Item2] = GetMinMaxSlice(grayImage, width, (((int, int))t).Item1, height), (fromY, s));

                Task.WaitAll(tasks);

                for (s = 0; s < numParallelThreads - 1; s++)
                {
                    if (minMaxs[s].Item1 < min)
                    {
                        min = minMaxs[s].Item1;
                    }

                    if (minMaxs[s].Item2 > max)
                    {
                        max = minMaxs[s].Item2;
                    }
                }

                ///////

                byte threshold = (byte)((max - min) * 0.5);
                fromY = 0;

                for (s = 0; s < numParallelThreads - 1; s++)
                {
                    tasks[s] = Task.Factory.StartNew((y) => BinarizeSlice(grayImage, width, (int)y, (int)y + step, threshold, bin), fromY);
                    fromY += step;
                }

                tasks[s] = Task.Factory.StartNew((y) => BinarizeSlice(grayImage, width, (int)y, height, threshold, bin), fromY);

                Task.WaitAll(tasks);
            }
            else
            {
                (min, max) = GetMinMaxSlice(grayImage, width, 0, height);
                byte threshold = (byte)((max - min) * 0.5);
                BinarizeSlice(grayImage, width, 0, height, threshold, bin);
            }

            return bin;
        }

        /////////////////////////////////////////////////////////////

        private (byte, byte) GetMinMaxSlice(byte[,] grayImage, int width, int fromY, int toY)
        {
            byte min = byte.MaxValue;
            byte max = byte.MinValue;

            for (int x = 0; x < width; x++)
            {
                for (int y = fromY; y < toY; y++)
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

            return (min, max);
        }

        /////////////////////////////////////////////////////////////

        private void BinarizeSlice(byte[,] grayImage, int width, int fromY, int toY, byte threshold, byte[,] bin)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = fromY; y < toY; y++)
                {
                    var val = grayImage[x, y];

                    bin[x, y] = val < threshold ? (byte)255 : (byte)0;
                }
            }
        }

        /////////////////////////////////////////////////////////////

        private unsafe byte[,] CreateGrayScale(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            byte* pixelArray = (byte*)(void*)bitmapData.Scan0;
            int stride = bitmapData.Stride;
            var pixelFormat = bitmap.PixelFormat;
            byte[,] grays = new byte[bitmap.Width, bitmap.Height];
            var brightnessPalette = bitmap.Palette;
            byte[] palette = null;
            Action<int, int> grayScaleSliceAction;
            int width = bitmap.Width;
            int height = bitmap.Height;

            ////////////////
            // use local functions because byte* is not possible with delegates

            unsafe void CreateGrayScale1bppIndexedSlice(int fromY, int toY)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = fromY; y < toY; y++)
                    {
                        byte indexes = pixelArray[y * stride + x / 8];
                        int index = ((indexes << (byte)(x % 8)) & 0x80) != 0 ? 1 : 0;
                        grays[x, y] = palette[index];
                    }
                }
            }

            ////////////////

            unsafe void CreateGrayScale4bppIndexedSlice(int fromY, int toY)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = fromY; y < toY; y++)
                    {
                        byte indexes = pixelArray[y * stride + x / 2];
                        int index;

                        if ((x % 2) != 0)
                        {
                            index = indexes & 0x0f;
                        }
                        else
                        {
                            index = indexes >> 4;
                        }

                        grays[x, y] = palette[index];
                    }
                }
            }

            ////////////////

            unsafe void CreateGrayScale8bppIndexedSlice(int fromY, int toY)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = fromY; y < toY; y++)
                    {
                        byte index = pixelArray[y * stride + x];
                        grays[x, y] = palette[index];
                    }
                }
            }

            ////////////////

            unsafe void CreateGrayScaleNoneIndexedSlice(int fromY, int toY)
            {
                int bytesPerPixel = pixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;

                for (int x = 0; x < width; x++)
                {
                    for (int y = fromY; y < toY; y++)
                    {
                        int pixelIndex = y * stride + x * bytesPerPixel;
                        var sum = pixelArray[pixelIndex] + pixelArray[pixelIndex + 1] + pixelArray[pixelIndex + 2];
                        grays[x, y] = (byte)(sum / 3);
                    }
                }
            }

            ////////////////

            switch (pixelFormat)
            {
                case PixelFormat.Format1bppIndexed:
                {
                    grayScaleSliceAction = CreateGrayScale1bppIndexedSlice;
                    break;
                }

                case PixelFormat.Format4bppIndexed:
                {
                    grayScaleSliceAction = CreateGrayScale4bppIndexedSlice;
                    break;
                }

                case PixelFormat.Format8bppIndexed:
                {
                    grayScaleSliceAction = CreateGrayScale8bppIndexedSlice;
                    break;
                }

                default: // 24bpp RGB, 32bpp formats
                    grayScaleSliceAction = CreateGrayScaleNoneIndexedSlice;
                    break;
            }

            if (brightnessPalette != null)
            {
                palette = new byte[brightnessPalette.Entries.Length];

                for (int i = 0; i < brightnessPalette.Entries.Length; i++)
                {
                    var color = brightnessPalette.Entries[i];
                    palette[i] = (byte)((color.R + color.G + color.B) / 3);
                }
            }

            if (numParallelThreads > 1)
            {
                Task[] tasks = new Task[numParallelThreads];

                int step = bitmap.Height / numParallelThreads;
                int FromY = 0;
                int s;

                for (s = 0; s < numParallelThreads - 1; s++)
                {
                    tasks[s] = Task.Factory.StartNew((y) => grayScaleSliceAction((int)y, (int)y + step), FromY);
                    FromY += step;
                }

                tasks[s] = Task.Factory.StartNew((y) => grayScaleSliceAction((int)y, height), FromY);

                Task.WaitAll(tasks);
            }
            else
            {
                grayScaleSliceAction(0, height);
            }

            bitmap.UnlockBits(bitmapData);

            return grays;
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
