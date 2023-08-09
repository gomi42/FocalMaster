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
using System.Windows.Documents;
using FocalXRomCodes;

namespace FocalCompiler
{
    public enum CompileResult
    {
        Ok,
        CompileError,
        UnknowStatement
    }

    partial class Compiler
    {
        private bool endProcessed = false;
        private XRomCodes xroms = new XRomCodes (true);
        private Lex lex = new Lex ();
        private Parameter parameter = new Parameter ();
        private bool lastStatementWasNumber;

        /////////////////////////////////////////////////////////////

        public bool IsEndDetected
        {
            get
            {
                return endProcessed;
            }
        }

        /////////////////////////////////////////////////////////////

        public void SetXromFile (string filename)
        {
            xroms.AddMnemonicsFromFile (filename);
        }

        /////////////////////////////////////////////////////////////

        private CompileResult CompileId (Token token, out byte[] outCode, out string errorMsg)
        {
            errorMsg = "";
            CompileResult result;

            // 1: check standard mnemonic
            result = CompileMnemonic (token, out outCode, out errorMsg);

            if (result != CompileResult.UnknowStatement)
                return result;
                    
            // 2: check XROM mnemonics
            result = CompileXRom (token, out outCode, out errorMsg);

            if (result != CompileResult.UnknowStatement)
                return result;

            // 3: check directive
            result = DoDirective (token, out outCode, out errorMsg);

            if (result != CompileResult.UnknowStatement)
                return result;

            errorMsg = string.Format ("Unknown statement \"{0}\"", token.StringValue);
            return CompileResult.UnknowStatement;
        }

        /////////////////////////////////////////////////////////////

        public bool CompileEnd (out byte[][] outCodes)
        {
            return Compile (".END.", out outCodes, out _);
        }

        /////////////////////////////////////////////////////////////

        public bool Compile (string line, out byte[][] outputCodes, out string errorMsg)
        {
            outputCodes = null;
            bool error = false;
            errorMsg = string.Empty;
            Token token = new Token ();

            lex.GetFirstToken (line, ref token);

            if (endProcessed)
            {
                switch (token.TokenType)
                {
                    case Token.TokType.Eol:
                    case Token.TokType.Comment:
                        outputCodes = new byte[0][];
                        return false;
                    
                    default:
                        outputCodes = new byte[0][];
                        errorMsg = "Statement after .END. detected";
                        return true;
                }
            }

            switch (token.TokenType)
            {
                case Token.TokType.Id:
                {
                    if (CompileId(token, out byte[] outCode, out errorMsg) == CompileResult.Ok)
                    {
                        if (outCode != null)
                        {
                            outputCodes = new byte[1][];
                            outputCodes[0] = outCode;
                        }
                    }
                    else
                    {
                        error = true;
                    }

                    lastStatementWasNumber = false;
                    break;
                }
                
                case Token.TokType.Append:
                {
                    if (CompileTextAppend(token, out byte[] outCode, out errorMsg) == CompileResult.Ok)
                    {
                        outputCodes = new byte[1][];
                        outputCodes[0] = outCode;
                    }
                    else
                    {
                        error = true;
                    }

                    lastStatementWasNumber = false;
                    break;
                }

                case Token.TokType.Text:
                {
                    if (CompileText(token, out byte[] outCode, out errorMsg) == CompileResult.Ok)
                    {
                        outputCodes = new byte[1][];
                        outputCodes[0] = outCode;
                    }
                    else
                    {
                        error = true;
                    }

                    lastStatementWasNumber = false;
                    break;
                }

                case Token.TokType.Int:
                case Token.TokType.Number:
                {
                    if (CompileNumber(token, out byte[] outCode, out errorMsg) == CompileResult.Ok)
                    {
                        if (lastStatementWasNumber)
                        {
                            var separatorCode = new byte[1];
                            separatorCode[0] = 0x00;

                            outputCodes = new byte[2][];
                            outputCodes[0] = separatorCode;
                            outputCodes[1] = outCode;
                        }
                        else
                        {
                            outputCodes = new byte[1][];
                            outputCodes[0] = outCode;
                        }
                    }
                    else
                    {
                        error = true;
                    }

                    lastStatementWasNumber = true;
                    break;
                }

                case Token.TokType.Eol:
                case Token.TokType.Comment:
                {
                    lastStatementWasNumber = false;
                    break;
                }

                default:
                {
                    lastStatementWasNumber = false;
                    error = true;
                    errorMsg = string.Format("Unknown statement \"{0}\"", token.StringValue);
                    break;
                }
            }

            if (!error)
            {
                lex.GetToken (ref token);

                if (token.TokenType != Token.TokType.Eol && token.TokenType != Token.TokType.Comment)
                {
                    outputCodes = null;
                    error = true;
                    errorMsg = string.Format("Unexpected parameter \"{0}\"", token.StringValue);
                }
            }

            return error;
        }
    }
}
