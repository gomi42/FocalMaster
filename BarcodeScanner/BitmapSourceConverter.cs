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
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FocalCompiler
{
    internal static class BitmapSourceConverter
    {
        public static BitmapSource GetBitmapSource(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                96, 96,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        public static BitmapSource GetBitmapSource(byte[,] image, List<Rectangle> rects = null)
        {
            var width = image.GetLength(0);
            var height = image.GetLength(1);
            PixelFormat pf = PixelFormats.Bgr24;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[stride * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var g = image[x, y];

                    int pixelIndex = y * stride + x * 3;
                    pixels[pixelIndex] = g;
                    pixels[pixelIndex + 1] = g;
                    pixels[pixelIndex + 2] = g;
                }
            }

            if (rects != null)
            {
                foreach (var re in rects)
                {
                    int row = re.Top;

                    for (int x = re.Left; x < re.Right; x++)
                    {
                        int pixelIndex = row * stride + x * 3;
                        pixels[pixelIndex] = 0;
                        pixels[pixelIndex + 1] = 0;
                        pixels[pixelIndex + 2] = 255;
                    }

                    row = re.Bottom;

                    for (int x = re.Left; x < re.Right; x++)
                    {
                        int pixelIndex = row * stride + x * 3;
                        pixels[pixelIndex] = 0;
                        pixels[pixelIndex + 1] = 0;
                        pixels[pixelIndex + 2] = 255;
                    }

                    int col = re.Left;

                    for (int y = re.Top; y < re.Bottom; y++)
                    {
                        int pixelIndex = y * stride + col * 3;
                        pixels[pixelIndex] = 0;
                        pixels[pixelIndex + 1] = 0;
                        pixels[pixelIndex + 2] = 255;
                    }

                    row = re.Right - 1;

                    for (int y = re.Top; y < re.Bottom; y++)
                    {
                        int pixelIndex = y * stride + row * 3;
                        pixels[pixelIndex] = 0;
                        pixels[pixelIndex + 1] = 0;
                        pixels[pixelIndex + 2] = 255;
                    }
                }
            }

            var resultBitmap = BitmapSource.Create(width, height, 96, 96, pf, null, pixels, stride);
            return resultBitmap;
        }

        public static BitmapSource GetBitmapSource(bool[,] image)
        {
            var width = image.GetLength(0);
            var height = image.GetLength(1);
            System.Windows.Media.PixelFormat pf = PixelFormats.Bgr24;
            int sobelStride = (width * pf.BitsPerPixel + 7) / 8;
            byte[] imagePixels = new byte[sobelStride * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte g = image[x, y] ? (byte)255 : (byte)0;

                    int pixelIndex = y * sobelStride + x * 3;
                    imagePixels[pixelIndex] = g;
                    imagePixels[pixelIndex + 1] = g;
                    imagePixels[pixelIndex + 2] = g;
                }
            }

            var resultBitmap = BitmapSource.Create(width, height, 96, 96, pf, null, imagePixels, sobelStride);
            return resultBitmap;
        }

    }
}
