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

namespace FocalCompiler
{
    partial class Compiler
    {
        CompileResult DoDirective (Token token, out byte[] outCode, out string errorMsg)
        {
            errorMsg = string.Empty;
            CompileResult result = CompileResult.Ok;

            switch (token.StringValue.ToLower())
            {
                case "define":
                {
                    string define;

                    lex.GetToken (ref token);

                    if (token.TokenType != Token.TokType.Id)
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format ("Identifier expected \"{0}\"", token.StringValue);
                        break;
                    }

                    define = token.StringValue;
                    lex.GetToken (ref token);

                    if (!( token.TokenType == Token.TokType.Text
                        || token.TokenType == Token.TokType.Letter
                        || token.TokenType == Token.TokType.Int
                        || token.TokenType == Token.TokType.Number))
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format ("Unexpected type \"{0}\"", token.StringValue);
                        break;
                    }

                    outCode = null;
                    lex.AddDefine (define, token);
                    break;
                }

                case "end":
                case ".end.":
                    outCode = new byte[3];
                    outCode[0] = 0xC0;
                    outCode[1] = 0x00;
                    outCode[2] = 0x2F;

                    endProcessed = true;
                    break;

                default:
                    outCode = null;
                    result = CompileResult.UnknowStatement;
                    break;
            }

            return result;
        }
    }
}
