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
            Errors = new List<string>();
            byte[] byteArray = Encoding.ASCII.GetBytes(focal);

            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return Compile(reader, outputFilename);
                }
            }
        }

        /////////////////////////////////////////////////////////////

        private bool Compile (StreamReader inFileStream, string outputFilename)
        {
            FileStream outFileStream;

            try
            {
                outFileStream = new FileStream (outputFilename, FileMode.Create);
            }
            catch
            {
                Errors.Add(string.Format("Cannot open output file: { 0}", outputFilename));
                return false;
            }

            /////////////////////////////

            Compiler compiler = new Compiler ();
            string exeFilename = Assembly.GetExecutingAssembly().Location;
            compiler.SetXromFile(Path.Combine(Path.GetDirectoryName(exeFilename), "XRomCodes.txt"));

            int outcodeLength;
            byte[] outCode = new byte[20];

            int lineNr = 1;

            string Line = inFileStream.ReadLine ();

            while (Line != null)
            {
                outcodeLength = 0;
                string errorMsg;

                if (compiler.Compile (Line, ref outcodeLength, ref outCode, out errorMsg))
                {
                    Errors.Add(string.Format ("Error line {0}: {1}", lineNr.ToString (), errorMsg));
                }

                if (Errors.Count == 0)
                {
                    for (int i = 0; i < outcodeLength; i++)
                    {
                        outFileStream.WriteByte (outCode[i]);
                    }
                }

                lineNr++;
                Line = inFileStream.ReadLine ();
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
