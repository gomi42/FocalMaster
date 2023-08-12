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
using System.Text;
using FocalCompiler;

namespace FocalMaster
{
    class FocalRawGenerator
    {
        public List<string> Errors { get; private set; }

        /////////////////////////////////////////////////////////////

        public bool CompileString(string focal, string outputFilename)
        {
            FileStream outFileStream;

            try
            {
                outFileStream = new FileStream(outputFilename, FileMode.Create);
            }
            catch
            {
                Errors.Add(string.Format("Cannot open output file: { 0}", outputFilename));
                return false;
            }

            /////////////////////////////
            
            Errors = new List<string>();

            Compiler compiler = new Compiler();
            string exeFilename = Assembly.GetExecutingAssembly().Location;
            compiler.SetXromFile(Path.Combine(Path.GetDirectoryName(exeFilename), "XRomCodes.txt"));

            int lineNr = 1;
            string[] lines = focal.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                string errorMsg;

                if (compiler.Compile(line, out byte[][] outCodes, out errorMsg))
                {
                    Errors.Add(string.Format("Error line {0}: {1}", lineNr.ToString(), errorMsg));
                }

                if (Errors.Count == 0 && outCodes != null)
                {
                    foreach (var outCode in outCodes)
                    {
                        int outcodeLength = outCode.Length;

                        for (int i = 0; i < outcodeLength; i++)
                        {
                            outFileStream.WriteByte(outCode[i]);
                        }
                    }
                }

                lineNr++;
            }

            outFileStream.Close();
            outFileStream.Dispose();

            if (Errors.Count > 0)
            {
                File.Delete(outputFilename);
            }

            return Errors.Count == 0;
        }
    }
}
