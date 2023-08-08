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
using System.IO;

namespace FocalCompiler
{
    internal abstract class BarcodeGenerator
    {
        const int MaxDataBytesPerRow = 16;
        const int NumHeaderBytesPerRow = 3;
        const int MaxCodeBytesPerRow = MaxDataBytesPerRow - NumHeaderBytesPerRow;


        /////////////////////////////

        private StreamWriter debugHexFileStream;
        private string debugHexFilename;
        private int lastChecksum;
        private int currentRow;

        private byte[] barcodeBuf;
        private int barcodeBufIndex = 0;
        private int trailing = 0;

        private int lineNr = 1;
        private int destLineNr = 1;
        private int destFromLine = 1;

        private Compiler compiler;

        private bool genHex;

        /////////////////////////////////////////////////////////////

        public List<string> Errors { get; private set; }

        /////////////////////////////////////////////////////////////

        protected bool Generate(StreamReader inFileStream, bool hexDebugOutput)
        {
            Errors = new List<string>();
            genHex = hexDebugOutput;

            if (hexDebugOutput)
            {
                debugHexFilename = Path.ChangeExtension("debug.test", ".hex");

                try
                {
                    debugHexFileStream = new StreamWriter(debugHexFilename, false, System.Text.Encoding.ASCII);
                }
                catch
                {
                    Errors.Add($"Cannot create debug file {debugHexFilename}");
                    return false;
                }
            }

            barcodeBuf = new byte[MaxCodeBytesPerRow];
            compiler = new Compiler();

            string line = inFileStream.ReadLine();

            while (line != null)
            {
                string ErrorMsg;

                if (compiler.Compile(line, out byte[][] outCodes, out ErrorMsg))
                {
                    Errors.Add(string.Format("{0}, line {1}, \"{2}\"", ErrorMsg, lineNr, line));
                }

                if (Errors.Count == 0)
                {
                    AddToBarcode(outCodes);
                }

                lineNr++;
                line = inFileStream.ReadLine();
            }

            if (Errors.Count == 0)
            {
                if (!compiler.IsEndDetected)
                {
                    compiler.CompileEnd(out byte[][] outCodes);
                    AddToBarcode(outCodes);
                }

                if (barcodeBufIndex > 0)
                {
                    OutputBarcode(barcodeBuf, barcodeBufIndex, 0, trailing, currentRow + 1, destFromLine, lineNr);
                }
            }

            inFileStream.Close();

            if (hexDebugOutput)
            {
                debugHexFileStream.Close();
                debugHexFileStream.Dispose();

                if (Errors.Count > 0)
                {
                    File.Delete(debugHexFilename);
                }
            }

            Save();

            return Errors.Count == 0;
        }

        /////////////////////////////////////////////////////////////

        private void AddToBarcode(byte[][] outCodes)
        {
            foreach (var outCode in outCodes)
            {
                AddToBarcode(outCode);
            }
        }

        /////////////////////////////////////////////////////////////

        private void AddToBarcode(byte[] outCode)
        {
            int outcodeLength = outCode.Length;

            if (outcodeLength > 0)
            {
                for (int i = 0; i < outcodeLength; i++)
                {
                    barcodeBuf[barcodeBufIndex] = outCode[i];
                    barcodeBufIndex++;

                    if (barcodeBufIndex == MaxCodeBytesPerRow)
                    {
                        int leading = 0;
                        int newTrailing = 0;
                        int newFromLine = destLineNr + 1;

                        if (outcodeLength != i + 1)
                        {
                            leading = i + 1;
                            newTrailing = outcodeLength - leading;
                            newFromLine = destLineNr;
                        }

                        OutputBarcode(barcodeBuf, MaxCodeBytesPerRow, leading, trailing, currentRow + 1, destFromLine, destLineNr);
                        trailing = newTrailing;
                        barcodeBufIndex = 0;
                        destFromLine = newFromLine;
                    }
                }

                destLineNr++;
            }
        }

        /////////////////////////////////////////////////////////////

        int CalcChecksum(byte[] bytes, int bufLen, int lastCheck)
        {
            int check = lastCheck;

            for (int i = 0; i < bufLen; i++)
            {
                check += bytes[i];

                if (check > 0xFF)
                {
                    check -= 0xFF;
                }
            }

            return check;
        }

        /////////////////////////////////////////////////////////////

        private void GenOneBarcode(byte[] barcodeBuf,
                                   int barcodeLength,
                                   int leading,
                                   int trailing,
                                   out byte[] barcodeOut,
                                   out int barcodeOutLen)
        {
            byte[] barcode = new byte[MaxCodeBytesPerRow + NumHeaderBytesPerRow];
            int destIndex = NumHeaderBytesPerRow;

            barcode[0] = (byte)lastChecksum;
            barcode[1] = (byte)((0x01 << 4) | (currentRow % 16));
            currentRow++;
            barcode[2] = (byte)((trailing << 4) | leading);

            for (int i = 0; i < barcodeLength; i++)
            {
                barcode[destIndex++] = barcodeBuf[i];
            }

            lastChecksum = CalcChecksum(barcode, barcodeLength + NumHeaderBytesPerRow, 0);
            barcode[0] = (byte)lastChecksum;

            barcodeOut = barcode;
            barcodeOutLen = barcodeLength + 3;
        }

        /////////////////////////////////////////////////////////////

        private void DumpBarcode(byte[] barcode, int barcodeLen, int currentRow, int fromLine, int toLine)
        {
            string res = string.Empty;

            for (int i = 0; i < barcodeLen; i++)
            {
                res += barcode[i].ToString("X2") + " ";
            }

            debugHexFileStream.WriteLine(string.Format("Row {0} ({1} - {2})", currentRow, fromLine, toLine));
            debugHexFileStream.WriteLine(res);

        }

        /////////////////////////////////////////////////////////////

        private void OutputBarcode(byte[] barcodeBuf, int barcodeLength, int leading, int trailing, int currentRow, int fromLine, int toLine)
        {
            byte[] barcode;
            int barcodeLen;

            GenOneBarcode(barcodeBuf, barcodeLength, leading, trailing, out barcode, out barcodeLen);
            AddBarcode(barcode, barcodeLen, currentRow, fromLine, toLine);

            if (genHex)
            {
                DumpBarcode(barcode, barcodeLen, currentRow, fromLine, toLine);
            }
        }

        protected abstract void AddBarcode(byte[] barcode, int barcodeLen, int currentRow, int fromLine, int toLine);

        protected abstract void Save();
    }
}
