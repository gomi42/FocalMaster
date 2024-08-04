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
        private Dictionary<string, short> stackParameter;
        private Dictionary<string, short> shortLabelParameter;

        /////////////////////////////////////////////////////////////

        public Parameter ()
        {
            stackParameter = new Dictionary<string, short> ();

            stackParameter.Add ("T", 112);
            stackParameter.Add ("Z", 113);
            stackParameter.Add ("Y", 114);
            stackParameter.Add ("X", 115);
            stackParameter.Add ("L", 116);
            stackParameter.Add ("M", 117);
            stackParameter.Add ("N", 118);
            stackParameter.Add ("O", 119);
            stackParameter.Add ("P", 120);
            stackParameter.Add ("Q", 121);
            stackParameter.Add ("R", 122);
            stackParameter.Add ("A", 123);
            stackParameter.Add ("B", 124);
            stackParameter.Add ("C", 125);
            stackParameter.Add ("D", 126);
            stackParameter.Add ("E", 127);

            shortLabelParameter = new Dictionary<string, short> ();

            shortLabelParameter.Add ("A", 102);
            shortLabelParameter.Add ("B", 103);
            shortLabelParameter.Add ("C", 104);
            shortLabelParameter.Add ("D", 105);
            shortLabelParameter.Add ("E", 106);
            shortLabelParameter.Add ("F", 107);
            shortLabelParameter.Add ("G", 108);
            shortLabelParameter.Add ("H", 109);
            shortLabelParameter.Add ("I", 110);
            shortLabelParameter.Add ("J", 111);
            shortLabelParameter.Add ("a", 123);
            shortLabelParameter.Add ("b", 124);
            shortLabelParameter.Add ("c", 125);
            shortLabelParameter.Add ("d", 126);
            shortLabelParameter.Add ("e", 127);
        }

        /////////////////////////////////////////////////////////////

        public bool GetStackParameter (string parameter, out short value)
        {
            value = 0;

            if (stackParameter.TryGetValue (parameter.ToUpper(), out value))
                return true;

            return false;
        }

        /////////////////////////////////////////////////////////////

        public bool GetShortLabelParameter (string parameter, out short value)
        {
            value = 0;

            if (shortLabelParameter.TryGetValue (parameter, out value))
                return true;

            return false;
        }
    }
}
