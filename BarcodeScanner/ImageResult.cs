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

using System.Collections.Generic;
using System.Drawing;

namespace FocalMaster
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
        NoBarcodeFound = 0,
        NoProgramCode = 1,
        InvalidSignature = 2,
        CheckSumError = 3,
        ProgramCode = 4
    }

    /////////////////////////////////////////////////////////////

    internal class ImageResult
    {
        public ImageResult(string filename, byte[,] grayImage, EdgeOrientation[,] edges, List<Rectangle> barcodeAreas, List<ScanResult> areaResults)
        {
            Filename = filename;
            GrayImage = grayImage;
            Edges = edges;
            BarcodeAreas = barcodeAreas;
            AreaResults = areaResults;
        }

        public ImageResult(string filename, byte[,] grayImage, List<Rectangle> barcodeAreas, List<ScanResult> areaResults)
        {
            Filename = filename;
            GrayImage = grayImage;
            Edges = null;
            BarcodeAreas = barcodeAreas;
            AreaResults = areaResults;
        }

        public ImageResult(string filename, byte[,] grayImage, List<Rectangle> barcodeAreas)
        {
            Filename = filename;
            GrayImage = grayImage;
            Edges = null;
            BarcodeAreas = barcodeAreas;
            AreaResults = null;
        }

        public ImageResult(string filename, byte[,] grayImage)
        {
            Filename = filename;
            GrayImage = grayImage;
            Edges = null;
            BarcodeAreas = null;
            AreaResults = null;
        }

        public ImageResult(string filename, EdgeOrientation[,] edges)
        {
            Filename = filename;
            GrayImage = null;
            Edges = edges;
            BarcodeAreas = null;
            AreaResults = null;
        }

        public string Filename { get; }
        public byte[,] GrayImage { get; }
        public EdgeOrientation[,] Edges { get; }
        public List<Rectangle> BarcodeAreas { get; }
        public List<ScanResult> AreaResults { get; }
    }
}
