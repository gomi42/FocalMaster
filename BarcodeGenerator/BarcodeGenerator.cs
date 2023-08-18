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
using System.IO;
using System.Reflection;

namespace FocalCompiler
{
    internal abstract class BarcodeGenerator
    {
        const int MaxDataBytesPerRow = 16;
        const int NumHeaderBytesPerRow = 3;
        const int MaxCodeBytesPerRow = MaxDataBytesPerRow - NumHeaderBytesPerRow;

        /////////////////////////////

        private int lastChecksum;
        private int currentRow;

        private byte[] barcodeBuf;
        private int barcodeBufIndex = 0;
        private int trailing = 0;

        private int currentStatementLineNr = 0;
        private int firstStatementLineNrOfBarcodeRow;

        private Compiler compiler;

        private bool genDebugHex;
        private string debugHexFilename;
        private StreamWriter debugHexFileStream;

        /////////////////////////////////////////////////////////////

        public List<string> Errors { get; private set; }

        /////////////////////////////////////////////////////////////

        protected bool Generate(string focal, bool hexDebugOutput)
        {
            Errors = new List<string>();
            genDebugHex = hexDebugOutput;
            
            if (!PrepareDebugOutput())
            {
                return false;
            }

            compiler = new Compiler();
            string exeFilename = Assembly.GetExecutingAssembly().Location;
            compiler.SetXromFile(Path.Combine(Path.GetDirectoryName(exeFilename), "XRomCodes.txt"));
            
            barcodeBuf = new byte[MaxCodeBytesPerRow];
            string[] lines = focal.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            int sourceLineNr = 1;

            foreach (var line in lines)
            {
                string ErrorMsg;

                if (compiler.Compile(line, out byte[][] outCodes, out ErrorMsg))
                {
                    Errors.Add(string.Format("{0}, line {1}, \"{2}\"", ErrorMsg, sourceLineNr, line));
                }

                if (Errors.Count == 0)
                {
                    AddToBarcodeRow(outCodes);
                }

                sourceLineNr++;
            }

            if (Errors.Count == 0)
            {
                if (!compiler.IsEndDetected)
                {
                    compiler.CompileEnd(out byte[][] outCodes);
                    AddToBarcodeRow(outCodes);
                }

                if (barcodeBufIndex > 0)
                {
                    FlushBarcodeRow(0);
                }
            }

            Save();
            FinalizeDebugOutput();

            return Errors.Count == 0;
        }

        /////////////////////////////////////////////////////////////

        private void AddToBarcodeRow(byte[][] outCodes)
        {
            if (outCodes != null)
            {
                foreach (var outCode in outCodes)
                {
                    AddToBarcodeRow(outCode);
                }
            }
        }

        /////////////////////////////////////////////////////////////

        private void AddToBarcodeRow(byte[] outCode)
        {
            int outcodeLength = outCode.Length;

            if (outcodeLength > 0)
            {
                currentStatementLineNr++;

                if (barcodeBufIndex == 0)
                {
                    firstStatementLineNrOfBarcodeRow = currentStatementLineNr;
                }

                for (int i = 0; i < outcodeLength; i++)
                {
                    barcodeBuf[barcodeBufIndex] = outCode[i];
                    barcodeBufIndex++;

                    if (barcodeBufIndex == MaxCodeBytesPerRow)
                    {
                        int leading = 0;
                        int nextTrailing = 0;

                        if (outcodeLength != i + 1)
                        {
                            leading = i + 1;
                            nextTrailing = outcodeLength - leading;

                            if (leading > MaxCodeBytesPerRow)
                            {
                                leading = MaxCodeBytesPerRow;
                            }

                            if (nextTrailing > MaxCodeBytesPerRow)
                            {
                                nextTrailing = MaxCodeBytesPerRow;
                            }
                        }

                        FlushBarcodeRow(leading);
                        trailing = nextTrailing;
                        barcodeBufIndex = 0;
                        firstStatementLineNrOfBarcodeRow = currentStatementLineNr;
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////

        private void GenerateFinalBarcodeRowData(int leading,
                                                 out byte[] barcodeOut)
        {
            byte[] barcode = new byte[barcodeBufIndex + NumHeaderBytesPerRow];

            barcode[0] = (byte)lastChecksum;
            barcode[1] = (byte)((0x01 << 4) | (currentRow % 16));
            barcode[2] = (byte)((trailing << 4) | leading);

            Array.Copy(barcodeBuf, 0, barcode, NumHeaderBytesPerRow, barcodeBufIndex);

            lastChecksum = CalcChecksum(barcode, barcodeBufIndex + NumHeaderBytesPerRow);
            barcode[0] = (byte)lastChecksum;

            barcodeOut = barcode;
        }

        /////////////////////////////////////////////////////////////

        private void FlushBarcodeRow(int leading)
        {
            GenerateFinalBarcodeRowData(leading, out byte[] barcode);
            AddBarcodeRow(barcode, currentRow + 1, firstStatementLineNrOfBarcodeRow, currentStatementLineNr);
            OutputDebugBarcodeRow(barcode);
            currentRow++;
        }

        /////////////////////////////////////////////////////////////

        private void AddBarcodeRow(byte[] barcode, int currentRow, int fromLine, int toLine)
        {
            BeginBarcodeRow(currentRow, fromLine, toLine);
            AddZeroZeroBar();
            AddBars(barcode);
            AddOneZeroBar();
            EndBarcodeRow();
        }

        /////////////////////////////////////////////////////////////

        protected virtual void AddBars(byte[] barcode)
        {
            int barcodeLen = barcode.Length;

            for (int i = 0; i < barcodeLen; i++)
            {
                byte b = barcode[i];

                for (int j = 0; j < 8; j++)
                {
                    if ((b & 0x80) == 0x80)
                    {
                        AddOneBar();
                    }
                    else
                    {
                        AddZeroBar();
                    }

                    b <<= 1;
                }
            }
        }

        /////////////////////////////////////////////////////////////

        protected virtual void AddZeroZeroBar()
        {
            AddZeroBar();
            AddZeroBar();
        }

        /////////////////////////////////////////////////////////////

        protected virtual void AddOneZeroBar()
        {
            AddOneBar();
            AddZeroBar();
        }

        /////////////////////////////////////////////////////////////

        protected virtual void AddOneBar()
        {
            throw new Exception("Implementation error. Override either AddOneBar, AddZeroZeroBar, AddOneZeroBar or AddBars");
        }

        /////////////////////////////////////////////////////////////

        protected virtual void AddZeroBar()
        {
            throw new Exception("Implementation error. Override either AddZeroBar, AddZeroZeroBar, AddOneZeroBar or AddBars");
        }

        /////////////////////////////////////////////////////////////

        protected abstract void BeginBarcodeRow(int currentRow, int fromLine, int toLine);
        
        protected abstract void EndBarcodeRow();

        protected abstract void Save();

        /////////////////////////////////////////////////////////////

        int CalcChecksum(byte[] bytes, int bufLen)
        {
            int check = 0;

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

        private bool PrepareDebugOutput()
        {
            if (genDebugHex)
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

            return true;
        }

        /////////////////////////////////////////////////////////////

        private void OutputDebugBarcodeRow(byte[] barcode)
        {
            if (genDebugHex)
            {
                string res = string.Empty;
                int barcodeLen = barcode.Length;

                for (int i = 0; i < barcodeLen; i++)
                {
                    res += barcode[i].ToString("X2") + " ";
                }

                debugHexFileStream.WriteLine(string.Format("Row {0} ({1} - {2})", currentRow + 1, firstStatementLineNrOfBarcodeRow, currentStatementLineNr));
                debugHexFileStream.WriteLine(res);
            }
        }

        /////////////////////////////////////////////////////////////

        private void FinalizeDebugOutput()
        {
            if (genDebugHex)
            {
                debugHexFileStream.Close();
                debugHexFileStream.Dispose();

                if (Errors.Count > 0)
                {
                    File.Delete(debugHexFilename);
                }
            }
        }
    }
}
