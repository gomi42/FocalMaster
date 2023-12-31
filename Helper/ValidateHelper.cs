﻿//
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
using FocalCompiler;

namespace FocalMaster.Helper
{
    internal static class ValidateHelper
    {
        public static List<string> Validate(string focal)
        {
            List<string> results = new List<string>();
            var compiler = new Compiler();
            string exeFilename = Assembly.GetExecutingAssembly().Location;
            compiler.SetXromFile(Path.Combine(Path.GetDirectoryName(exeFilename), "XRomCodes.txt"));

            int lineNr = 1;

            var lines = focal.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                string ErrorMsg;

                if (compiler.Compile(line, out byte[][] outCode, out ErrorMsg))
                {
                    results.Add(string.Format("{0}, line {1}, \"{2}\"", ErrorMsg, lineNr, line));
                }

                lineNr++;
            }

            if (results.Count == 0)
            {
                results.Add("ok");
            }

            return results;
        }
    }
}
