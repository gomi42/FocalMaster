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
using FocalXRomCodes;

namespace FocalDecompiler
{
    public class Decompiler
    {
        private Dictionary<short, string> stackParamter;
        private Dictionary<short, string> shortLabelParamter;

        /////////////////////////////////////////////////////////////

        void InitParameter()
        {
            stackParamter = new Dictionary<short, string>();

            stackParamter.Add(112, "T");
            stackParamter.Add(113, "Z");
            stackParamter.Add(114, "Y");
            stackParamter.Add(115, "X");
            stackParamter.Add(116, "L");
            stackParamter.Add(117, "M");
            stackParamter.Add(118, "N");
            stackParamter.Add(119, "O");
            stackParamter.Add(120, "P");
            stackParamter.Add(121, "Q");
            stackParamter.Add(122, "R");
            stackParamter.Add(123, "a");
            stackParamter.Add(124, "b");
            stackParamter.Add(125, "c");
            stackParamter.Add(126, "d");
            stackParamter.Add(127, "e");

            shortLabelParamter = new Dictionary<short, string>();

            shortLabelParamter.Add(102, "A");
            shortLabelParamter.Add(103, "B");
            shortLabelParamter.Add(104, "C");
            shortLabelParamter.Add(105, "D");
            shortLabelParamter.Add(106, "E");
            shortLabelParamter.Add(107, "F");
            shortLabelParamter.Add(108, "G");
            shortLabelParamter.Add(109, "H");
            shortLabelParamter.Add(110, "I");
            shortLabelParamter.Add(111, "J");

            shortLabelParamter.Add(112, "T");
            shortLabelParamter.Add(113, "Z");
            shortLabelParamter.Add(114, "Y");
            shortLabelParamter.Add(115, "X");
            shortLabelParamter.Add(116, "L");
            shortLabelParamter.Add(117, "M");
            shortLabelParamter.Add(118, "N");
            shortLabelParamter.Add(119, "O");
            shortLabelParamter.Add(120, "P");
            shortLabelParamter.Add(121, "Q");
            shortLabelParamter.Add(122, "R");

            shortLabelParamter.Add(123, "a");
            shortLabelParamter.Add(124, "b");
            shortLabelParamter.Add(125, "c");
            shortLabelParamter.Add(126, "d");
            shortLabelParamter.Add(127, "e");
        }

        /////////////////////////////////////////////////////////////

        public void Decompile(List<byte> code, out string focal)
        {
            MemoryStream inputStream;
            StringWriter outputStream;

            try
            {
                inputStream = new MemoryStream(code.ToArray());
            }
            catch
            {
                focal = null;
                return;
            }

            try
            {
                outputStream = new StringWriter();
            }
            catch
            {
                focal = null;
                return;
            }

            Decompile(inputStream, outputStream);

            focal = outputStream.ToString();

            inputStream.Close();
            outputStream.Close();
        }

        /////////////////////////////////////////////////////////////

        public void Decompile(string inputFilename, out string focal)
        {
            FileStream inputStream;
            StringWriter outputStream;

            try
            {
                inputStream = new FileStream(inputFilename, FileMode.Open);
            }
            catch
            {
                focal = null;
                return;
            }

            try
            {
                outputStream = new StringWriter();
            }
            catch
            {
                focal = null;
                return;
            }

            Decompile(inputStream, outputStream);

            focal = outputStream.ToString();

            inputStream.Close();
            outputStream.Close();
        }

        /////////////////////////////////////////////////////////////

        public void Decompile(string inputFilename, string outputFilename)
        {
            FileStream inputStream;
            StreamWriter outputStream;

            try
            {
                inputStream = new FileStream(inputFilename, FileMode.Open);
            }
            catch
            {
                Console.WriteLine(string.Format("Cannot open intput file: {0}", inputFilename));
                return;
            }

            try
            {
                outputStream = new StreamWriter(outputFilename, false, System.Text.Encoding.ASCII);
            }
            catch
            {
                Console.WriteLine(string.Format("Cannot open output file: {0}", outputFilename));
                return;
            }

            Decompile(inputStream, outputStream);

            inputStream.Close();
            outputStream.Close();
        }

        /////////////////////////////////////////////////////////////

        private void Decompile(Stream inputStream, TextWriter outputStream)
        {
            InitParameter();

            ///////////////////////////////

            int byteFromFile;
            string mnemonic;
            bool haveNextByte = false;
            OpCodes opCodes = new FocalDecompiler.OpCodes();
            XRomCodes xromCodes = new XRomCodes(false);

            string exeFilename = Assembly.GetExecutingAssembly().Location;
            xromCodes.AddMnemonicsFromFile(Path.Combine(Path.GetDirectoryName(exeFilename), "XRomCodes.txt"));

            byteFromFile = inputStream.ReadByte();

            int[] dump = new int[50];
            int dumpidx = 0;

            try
            {
                while (byteFromFile != -1)
                {
                    dumpidx = 0;
                    dump[dumpidx++] = byteFromFile;

                    OpCodes.OpCode OpCode = opCodes.GetOpCodeInfo(byteFromFile);

                    switch (OpCode.FctType)
                    {
                        case FocalDecompiler.OpCodes.FctType.Null:
                            break;

                        case FocalDecompiler.OpCodes.FctType.NoParam:
                            outputStream.WriteLine(OpCode.Mnemonic);
                            break;

                        case FocalDecompiler.OpCodes.FctType.R_0_9:
                        {
                            string ind = string.Empty;

                            byteFromFile = inputStream.ReadByte();
                            dump[dumpidx++] = byteFromFile;

                            if ((byteFromFile & 0x80) != 0)
                            {
                                byteFromFile &= 0x7f;
                                ind = "IND ";
                            }

                            if (byteFromFile <= 101)
                                outputStream.WriteLine("{0,-4} {1}{2}", OpCode.Mnemonic, ind, byteFromFile.ToString("D1"));
                            else
                                outputStream.WriteLine("{0,-4} {1}{2}", OpCode.Mnemonic, ind, stackParamter[(short)byteFromFile]);

                            break;
                        }

                        case FocalDecompiler.OpCodes.FctType.R_0_55:
                        case FocalDecompiler.OpCodes.FctType.R_0_101_Stack:
                        {
                            string ind = string.Empty;

                            byteFromFile = inputStream.ReadByte();
                            dump[dumpidx++] = byteFromFile;

                            if ((byteFromFile & 0x80) != 0)
                            {
                                byteFromFile &= 0x7f;
                                ind = "IND ";
                            }

                            if (byteFromFile <= 101)
                                outputStream.WriteLine("{0,-4} {1}{2}", OpCode.Mnemonic, ind, byteFromFile.ToString("D2"));
                            else
                                outputStream.WriteLine("{0,-4} {1}{2}", OpCode.Mnemonic, ind, stackParamter[(short)byteFromFile]);

                            break;
                        }

                        case FocalDecompiler.OpCodes.FctType.R_0_15:
                            outputStream.WriteLine("{0,-4} {1}", OpCode.Mnemonic, (byteFromFile & 0x0F).ToString("D2"));
                            break;

                        case FocalDecompiler.OpCodes.FctType.R_0_14:
                            outputStream.WriteLine("{0,-4} {1}", OpCode.Mnemonic, ((byteFromFile & 0x0F) - 1).ToString("D2"));
                            break;

                        case FocalDecompiler.OpCodes.FctType.GTO_0_14:
                            outputStream.WriteLine("{0,-4} {1}", OpCode.Mnemonic, ((byteFromFile & 0x0F) - 1).ToString("D2"));
                            byteFromFile = inputStream.ReadByte();
                            break;

                        case FocalDecompiler.OpCodes.FctType.R_0_99_A_J:
                            byteFromFile = inputStream.ReadByte();
                            dump[dumpidx++] = byteFromFile;
                            byteFromFile = inputStream.ReadByte();
                            dump[dumpidx++] = byteFromFile;

                            byteFromFile &= 0x7F;

                            if (byteFromFile <= 101)
                                outputStream.WriteLine("{0,-4} {1}", OpCode.Mnemonic, byteFromFile.ToString("D2"));
                            else
                                outputStream.WriteLine("{0,-4} {1}", OpCode.Mnemonic, shortLabelParamter[(short)byteFromFile]);

                            break;

                        case FocalDecompiler.OpCodes.FctType.LBL_0_99_A_J:
                            byteFromFile = inputStream.ReadByte();
                            dump[dumpidx++] = byteFromFile;

                            if (byteFromFile <= 101)
                                outputStream.WriteLine("{0,-4} {1}", OpCode.Mnemonic, byteFromFile.ToString("D2"));
                            else
                                outputStream.WriteLine("{0,-4} {1}", OpCode.Mnemonic, shortLabelParamter[(short)byteFromFile]);

                            break;

                        case FocalDecompiler.OpCodes.FctType.GTO_XEQ_Ind:
                            byteFromFile = inputStream.ReadByte();
                            dump[dumpidx++] = byteFromFile;

                            if ((byteFromFile & 0x80) == 0)
                                mnemonic = "GTO";
                            else
                                mnemonic = "XEQ";

                            byteFromFile &= 0x7f;

                            if (byteFromFile <= 101)
                                outputStream.WriteLine("{0,-4} IND {1}", mnemonic, byteFromFile.ToString("D2"));
                            else
                                outputStream.WriteLine("{0,-4} IND {1}", mnemonic, stackParamter[(short)byteFromFile]);

                            break;

                        case FocalDecompiler.OpCodes.FctType.XRom:
                        {
                            int byte2;
                            int module;
                            int function;
                            XRomCode xromCode;

                            byte2 = inputStream.ReadByte();
                            dump[dumpidx++] = byteFromFile;
                            module = ((byteFromFile & 0x07) << 2) | (byte2 >> 6);
                            function = byte2 & 0x3f;

                            if (xromCodes.FindFunction(module, function, out xromCode))
                                outputStream.WriteLine("{0}", xromCode.Mnemonic);
                            else
                                outputStream.WriteLine("{0,-4} {1},{2}", OpCode.Mnemonic, module.ToString("D2"), function.ToString("D2"));
                            break;
                        }

                        case FocalDecompiler.OpCodes.FctType.LabelAlpha:
                        {
                            int len;
                            string label = string.Empty;

                            var chain = inputStream.ReadByte();
                            dump[dumpidx++] = chain;

                            len = inputStream.ReadByte();
                            dump[dumpidx++] = len;

                            var keyAssignment = inputStream.ReadByte();
                            dump[dumpidx++] = keyAssignment;

                            if ((len & 0xF0) == 0xF0)
                            {
                                len = (len & 0x0F) - 1;

                                while (len-- > 0)
                                {
                                    byteFromFile = inputStream.ReadByte();
                                    dump[dumpidx++] = byteFromFile;
                                    label += (char)byteFromFile;
                                }

                                outputStream.WriteLine("{0,-4} \"{1}\"", OpCode.Mnemonic, label);
                            }
                            else
                            {
                                if ((len & 0x20) != 0)
                                {
                                    outputStream.WriteLine(".END.");
                                }
                                else
                                {
                                    outputStream.WriteLine("END");
                                }
                            }

                            break;
                        }

                        case FocalDecompiler.OpCodes.FctType.GTO_XEQ_Alpha:
                        {
                            int Len;
                            string label = string.Empty;

                            Len = inputStream.ReadByte() - 0xf0;
                            dump[dumpidx++] = byteFromFile;

                            while (Len-- > 0)
                            {
                                byteFromFile = inputStream.ReadByte();
                                dump[dumpidx++] = byteFromFile;
                                label += (char)byteFromFile;
                            }

                            outputStream.WriteLine("{0,-4} \"{1}\"", OpCode.Mnemonic, label);
                            break;
                        }

                        case FocalDecompiler.OpCodes.FctType.Alpha:
                        {
                            int len;
                            string label = string.Empty;
                            string append = string.Empty;

                            len = byteFromFile & 0x0f;
                            byteFromFile = inputStream.ReadByte();
                            dump[dumpidx++] = byteFromFile;

                            if (byteFromFile == 0x7f) //append
                            {
                                append = ">";
                                byteFromFile = inputStream.ReadByte();
                                dump[dumpidx++] = byteFromFile;
                                len--;
                            }

                            while (len-- > 0)
                            {
                                label += (char)byteFromFile;
                                byteFromFile = inputStream.ReadByte();
                                dump[dumpidx++] = byteFromFile;
                            }

                            outputStream.WriteLine("{0}\"{1}\"", append, label);
                            haveNextByte = true;
                            break;
                        }

                        case FocalDecompiler.OpCodes.FctType.Number:
                            do
                            {
                                if (byteFromFile == 0x1b)
                                    outputStream.Write("E");
                                else
                                    if (byteFromFile == 0x1a)
                                    outputStream.Write(".");
                                else
                                        if (byteFromFile == 0x1c)
                                    outputStream.Write("-");
                                else
                                    outputStream.Write("{0}", (char)(byteFromFile - 0x10 + '0'));

                                byteFromFile = inputStream.ReadByte();
                                dump[dumpidx++] = byteFromFile;
                                OpCode = opCodes.GetOpCodeInfo(byteFromFile);
                            }
                            while (OpCode.FctType == FocalDecompiler.OpCodes.FctType.Number);

                            outputStream.WriteLine();
                            haveNextByte = true;
                            break;

                        default:
                            outputStream.WriteLine("? {0}", byteFromFile.ToString("X2"));
                            break;
                    }

                    if (!haveNextByte)
                    {
                        byteFromFile = inputStream.ReadByte();
                        dump[dumpidx++] = byteFromFile;
                    }

                    haveNextByte = false;
                }
            }
            catch (Exception e)
            {
                Console.Write("Exception caught on statement: ");

                for (int i = 0; i < dumpidx; i++)
                {
                    Console.Write("{0} ", dump[i]);
                }

                Console.WriteLine();
            }


            inputStream.Close();
            outputStream.Close();
        }
    }
}
