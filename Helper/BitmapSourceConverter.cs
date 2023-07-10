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

        public static BitmapSource GetBitmapSource(byte[,] image, List<Rectangle> rects, List<ScanResult> areaResults)
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

            AddRectangels(rects, areaResults, stride, pixels);

            var resultBitmap = BitmapSource.Create(width, height, 96, 96, pf, null, pixels, stride);
            return resultBitmap;
        }

        private static void AddRectangels(List<Rectangle> rects, List<ScanResult> areaResults, int stride, byte[] pixels)
        {
            if (rects != null)
            {
                for (int i = 0; i < rects.Count; i++)
                {
                    byte redBorder;
                    byte greenBorder;
                    byte blueBorder;
                    byte redFill;
                    byte greenFill;
                    byte blueFill;

                    if (areaResults != null)
                    {
                        if (i < areaResults.Count)
                        {
                            var result = areaResults[i];

                            switch (result)
                            {
                                case ScanResult.ProgramCode:
                                    redBorder = 0;
                                    greenBorder = 255;
                                    blueBorder = 0;
                                    break;

                                case ScanResult.NoProgramCode:
                                    redBorder = 255;
                                    greenBorder = 0;
                                    blueBorder = 255;
                                    break;

                                case ScanResult.CheckSumError:
                                    redBorder = 255;
                                    greenBorder = 0;
                                    blueBorder = 0;
                                    break;

                                default:
                                    redBorder = 0;
                                    greenBorder = 255;
                                    blueBorder = 255;
                                    break;

                            }
                        }
                        else
                        {
                            redBorder = 0;
                            greenBorder = 0;
                            blueBorder = 255;
                        }
                    }
                    else
                    {
                        redBorder = 255;
                        greenBorder = 0;
                        blueBorder = 0;
                    }


                    redFill = redBorder;
                    greenFill = greenBorder;
                    blueFill = blueBorder;

                    if (redFill == 0)
                        redFill = 128;

                    if (greenFill == 0)
                        greenFill = 128;

                    if (blueFill == 0)
                        blueFill = 128;


                    var re = rects[i];

                    double factor = 0.2;
                    double factor1 = 1 - factor;

                    for (int x = re.Left; x <= re.Right; x++)
                    {
                        for (int y = re.Top; y <= re.Bottom; y++)
                        {
                            int pixelIndex = y * stride + x * 3;

                            pixels[pixelIndex] = (byte)(pixels[pixelIndex] * factor1 + blueFill * factor);
                            pixels[pixelIndex + 1] = (byte)(pixels[pixelIndex + 1] * factor1 + greenFill * factor);
                            pixels[pixelIndex + 2] = (byte)(pixels[pixelIndex + 2] * factor1 + redFill * factor);
                        }
                    }

                    int row = re.Top - 1;

                    if (row >= 0)
                    {
                        for (int x = re.Left - 1; x < re.Right + 1; x++)
                        {
                            int pixelIndex = row * stride + x * 3;
                            pixels[pixelIndex] = blueBorder;
                            pixels[pixelIndex + 1] = greenBorder;
                            pixels[pixelIndex + 2] = redBorder;
                        }

                        row = re.Bottom + 1;

                        for (int x = re.Left - 1; x < re.Right + 1; x++)
                        {
                            int pixelIndex = row * stride + x * 3;
                            pixels[pixelIndex] = blueBorder;
                            pixels[pixelIndex + 1] = greenBorder;
                            pixels[pixelIndex + 2] = redBorder;
                        }

                        int col = re.Left - 1;

                        for (int y = re.Top; y < re.Bottom + 1; y++)
                        {
                            int pixelIndex = y * stride + col * 3;
                            pixels[pixelIndex] = blueBorder;
                            pixels[pixelIndex + 1] = greenBorder;
                            pixels[pixelIndex + 2] = redBorder;
                        }

                        col = re.Right + 1;

                        for (int y = re.Top; y < re.Bottom + 1; y++)
                        {
                            int pixelIndex = y * stride + col * 3;
                            pixels[pixelIndex] = blueBorder;
                            pixels[pixelIndex + 1] = greenBorder;
                            pixels[pixelIndex + 2] = redBorder;
                        }
                    }
                }
            }
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
