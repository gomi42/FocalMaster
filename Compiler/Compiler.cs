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

        private CompileResult CompileId (Token token, ref int outCodeLength, ref byte[] outCode, out String errorMsg)
        {
            errorMsg = "";
            CompileResult result;

            // 1: check standard mnemonic
            result = CompileMnemonic (token, ref outCodeLength, ref outCode, out errorMsg);

            if (result != CompileResult.UnknowStatement)
                return result;
                    
            // 2: check XROM mnemonics
            result = CompileXRom (token, ref outCodeLength, ref outCode, out errorMsg);

            if (result != CompileResult.UnknowStatement)
                return result;

            // 3: check directive
            result = DoDirective (token, ref outCodeLength, ref outCode, out errorMsg);

            if (result != CompileResult.UnknowStatement)
                return result;

            errorMsg = string.Format ("Unknown statement \"{0}\"", token.StringValue);
            return CompileResult.UnknowStatement;
        }

        /////////////////////////////////////////////////////////////

        public bool CompileEnd (ref int outCodeLength, ref byte[] outCode)
        {
            string errorMsg;

            return Compile (".END.", ref outCodeLength, ref outCode, out errorMsg);
        }

        /////////////////////////////////////////////////////////////

        public bool Compile (String line, ref int outCodeLength, ref byte [] outCode, out String errorMsg)
        {
            bool error = false;
            errorMsg = "";
            Token token = new Token ();

            lex.GetFirstToken (line, ref token);

            if (endProcessed)
            {
                switch (token.TokenType)
                {
                    case Token.TokType.Eol:
                    case Token.TokType.Comment:
                        return false;
                    
                    default:
                        errorMsg = "Statement after .END. detected";
                        return true;
                }
            }

            switch (token.TokenType)
            {
                case Token.TokType.Id:
                    if (CompileId(token, ref outCodeLength, ref outCode, out errorMsg) != CompileResult.Ok)
                    {
                        error = true;
                    }

                    lastStatementWasNumber = false;
                    break;
                
                case Token.TokType.Append:
                    if (CompileTextAppend(token, ref outCodeLength, ref outCode, out errorMsg) != CompileResult.Ok)
                    {
                        error = true;
                    }

                    lastStatementWasNumber = false;
                    break;

                case Token.TokType.Text:
                    if (CompileText(token, ref outCodeLength, ref outCode, out errorMsg) != CompileResult.Ok)
                    {
                        error = true;
                    }

                    lastStatementWasNumber = false;
                    break;

                case Token.TokType.Int:
                case Token.TokType.Number:
                    outCodeLength = 0;

                    if (lastStatementWasNumber)
                    {
                        outCode[0] = 0x00;
                        outCodeLength++;
                    }

                    if (CompileNumber(token, ref outCodeLength, ref outCode, out errorMsg) != CompileResult.Ok)
                    {
                        error = true;
                    }

                    lastStatementWasNumber = true;
                    break;

                case Token.TokType.Eol:
                case Token.TokType.Comment:
                    lastStatementWasNumber = false;
                    break;

                default:
                    error = true;
                    errorMsg = string.Format ("Unknown statement \"{0}\"", token.StringValue);
                    break;
            }

            if (!error)
            {
                lex.GetToken (ref token);

                if (token.TokenType != Token.TokType.Eol && token.TokenType != Token.TokType.Comment)
                {
                    error = true;
                    errorMsg = string.Format ("Unexpected parameter \"{0}\"", token.StringValue);
                }
            }

            return error;
        }
    }
}
