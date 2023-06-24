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
using FocalCompiler;

namespace FocalMaster.Helper
{
    internal static class ValidateHelper
    {
        public static List<string> Validate(string focal)
        {
            List<string> results = new List<string>();
            byte[] outCode = new byte[20];
            var compiler = new Compiler();
            int lineNr = 1;

            var lines = focal.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string ErrorMsg;
                var outcodeLength = 0;

                if (compiler.Compile(line, ref outcodeLength, ref outCode, out ErrorMsg))
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
