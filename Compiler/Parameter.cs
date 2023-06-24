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
using System.Text;

namespace FocalCompiler
{
    public class Parameter
    {
        private Dictionary<String, short> stackParamter;
        private Dictionary<String, short> shortLabelParamter;

        /////////////////////////////////////////////////////////////

        public Parameter ()
        {
            stackParamter = new Dictionary<string, short> ();

            stackParamter.Add ("T", 112);
            stackParamter.Add ("Z", 113);
            stackParamter.Add ("Y", 114);
            stackParamter.Add ("X", 115);
            stackParamter.Add ("L", 116);
            stackParamter.Add ("M", 117);
            stackParamter.Add ("N", 118);
            stackParamter.Add ("O", 119);
            stackParamter.Add ("P", 120);
            stackParamter.Add ("Q", 121);
            stackParamter.Add ("R", 122);
            stackParamter.Add ("A", 123);
            stackParamter.Add ("B", 124);
            stackParamter.Add ("C", 125);
            stackParamter.Add ("D", 126);
            stackParamter.Add ("E", 127);

            shortLabelParamter = new Dictionary<string, short> ();

            shortLabelParamter.Add ("A", 102);
            shortLabelParamter.Add ("B", 103);
            shortLabelParamter.Add ("C", 104);
            shortLabelParamter.Add ("D", 105);
            shortLabelParamter.Add ("E", 106);
            shortLabelParamter.Add ("F", 107);
            shortLabelParamter.Add ("G", 108);
            shortLabelParamter.Add ("H", 109);
            shortLabelParamter.Add ("I", 110);
            shortLabelParamter.Add ("J", 111);
            shortLabelParamter.Add ("a", 123);
            shortLabelParamter.Add ("b", 124);
            shortLabelParamter.Add ("c", 125);
            shortLabelParamter.Add ("d", 126);
            shortLabelParamter.Add ("e", 127);
        }

        /////////////////////////////////////////////////////////////

        public bool GetStackParamter (string parameter, out short value)
        {
            value = 0;

            if (stackParamter.TryGetValue (parameter.ToUpper(), out value))
                return true;

            return false;
        }

        /////////////////////////////////////////////////////////////

        public bool GetShortLabelParamter (string parameter, out short value)
        {
            value = 0;

            if (shortLabelParamter.TryGetValue (parameter, out value))
                return true;

            return false;
        }
    }
}
