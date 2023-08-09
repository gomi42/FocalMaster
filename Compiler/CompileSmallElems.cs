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
    partial class Compiler
    {
        private Dictionary<char, char> CharConv = new Dictionary<char, char> ();

        CompileResult CompileTextAppend (Token token, out byte[] outCode, out string errorMsg)
        {
            lex.GetToken (ref token);

            if (token.TokenType != Token.TokType.Text)
            {
                outCode = new byte[0];
                errorMsg = string.Format ("Text expected \"{0}\"", token.StringValue);
                return CompileResult.CompileError;
            }

            return CompileText (token, out outCode, out errorMsg, true);
        }

        /////////////////////////////////////////////////////////////

        CompileResult CompileText (Token token, out byte[] outCode, out string errorMsg, bool append = false)
        {
            CompileResult result = CompileResult.Ok;
            errorMsg = string.Empty;
            int tokenLength = token.StringValue.Length;

            if (append)
            {
                tokenLength++;
            }

            if (tokenLength <= 15)
            {
                outCode = new byte[tokenLength + 1];
                outCode[0] = (byte)(0xF0 + tokenLength);

                int outCodeLength = 1;

                if (append)
                {
                    outCode[outCodeLength++] = (byte)0x7f;  //append
                }

                foreach (char c in token.StringValue)
                {
                    outCode[outCodeLength++] = (byte)c;
                }
            }
            else
            {
                outCode = null;
                result = CompileResult.CompileError;
                errorMsg = string.Format ("String to too long \"{0}\"", token.StringValue);
            }

            return result;
        }

        /////////////////////////////////////////////////////////////

        CompileResult CompileNumber (Token token, out byte[] outCode, out string errorMsg)
        {
            CompileResult result = CompileResult.Ok;
            errorMsg = string.Empty;
            outCode = new byte[token.StringValue.Length];
            int outCodeLength = 0;

            foreach (char c in token.StringValue)
            {
                if (c == '-')
                {
                    outCode[outCodeLength] = 0x1C;
                }
                else if (c == 'E' || c == 'e')
                {
                    outCode[outCodeLength] = 0x1B;
                }
                else if (c == '.')
                {
                    outCode[outCodeLength] = 0x1A;
                }
                else
                {
                    outCode[outCodeLength] = (byte)((byte)0x10 + (byte)c - (byte)'0');
                }

                outCodeLength++;
            }

            return result;
        }
    }
}
