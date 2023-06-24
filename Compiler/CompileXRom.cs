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
using FocalXRomCodes;

namespace FocalCompiler
{
    partial class Compiler
    {
        CompileResult CompileXRom (Token token, ref int outCodeLength, ref byte[] outCode, out string errorMsg)
        {
            errorMsg = string.Empty;
            XRomCode xromCode;

            if (!xroms.FindMnemonic (token.StringValue, out xromCode))
            {
                return CompileResult.UnknowStatement;
            }

            outCodeLength = 2;

            outCode[0] = (byte)(0xA0 + ((byte)xromCode.Rom >> 2));
            outCode[1] = (byte)((((byte)xromCode.Rom & 0x03) << 6) + (byte)xromCode.Function);

            return CompileResult.Ok;
        }
    }
}
