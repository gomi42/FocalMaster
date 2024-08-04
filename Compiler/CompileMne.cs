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
using FocalMaster.Helper;

namespace FocalCompiler
{
    partial class Compiler
    {
        private OpCodes opCodes = new OpCodes();

        /////////////////////////////////////////////////////////////

        CompileResult CompileMnemonicType2(OpCode opCode, out byte[] outCode, out string errorMsg)
        {
            CompileResult result = CompileResult.Ok;
            errorMsg = string.Empty;
            Token token = new Token();

            lex.GetToken(ref token);

            switch (token.TokenType)
            {
                case Token.TokType.Int:
                    if (opCode.ShortParamRange == FctType.R_0_14 && 0 <= token.IntValue && token.IntValue <= 14)
                    {
                        outCode = new byte[1];
                        outCode[0] = (byte)(((byte)opCode.ShortFunction + (byte)token.IntValue) & 0xff);
                    }
                    else
                    if (opCode.ShortParamRange == FctType.R_0_15 && 0 <= token.IntValue && token.IntValue <= 15)
                    {
                        outCode = new byte[1];
                        outCode[0] = (byte)(((byte)opCode.ShortFunction | (byte)token.IntValue) & 0xff);
                    }
                    else
                    if (0 <= token.IntValue && token.IntValue <= 101)
                    {
                        outCode = new byte[2];
                        outCode[0] = (byte)(opCode.Function & 0xff);
                        outCode[1] = (byte)(token.IntValue & 0xff);
                    }
                    else
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                    }
                    break;

                case Token.TokType.Letter:
                {
                    short Value;

                    if (parameter.GetStackParameter(token.StringValue, out Value))
                    {
                        outCode = new byte[2];
                        outCode[0] = (byte)(opCode.Function & 0xff);
                        outCode[1] = (byte)(Value & 0xff);
                    }
                    else
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format("Wrong stack parameter \"{0}\"", token.StringValue);
                    }
                    break;
                }

                case Token.TokType.Indirect:
                    lex.GetToken(ref token);

                    switch (token.TokenType)
                    {
                        case Token.TokType.Int:
                            outCode = new byte[2];
                            outCode[0] = (byte)(opCode.Function & 0xff);
                            outCode[1] = (byte)((token.IntValue & 0xff) | 0x80);
                            break;

                        case Token.TokType.Letter:
                        {
                            short Value;

                            if (parameter.GetStackParameter(token.StringValue, out Value))
                            {
                                outCode = new byte[2];
                                outCode[0] = (byte)(opCode.Function & 0xff);
                                outCode[1] = (byte)((Value & 0xff) | 0x80);
                            }
                            else
                            {
                                outCode = null;
                                result = CompileResult.CompileError;
                                errorMsg = string.Format("Wrong stack parameter \"{0}\"", token.StringValue);
                            }
                            break;
                        }

                        default:
                            outCode = null;
                            result = CompileResult.CompileError;
                            errorMsg = string.Format("Wrong parameter type or parameter expected");
                            break;
                    }
                    break;

                default:
                    outCode = null;
                    result = CompileResult.CompileError;
                    errorMsg = string.Format("Wrong parameter type or parameter expected");
                    break;
            }

            return result;
        }

        /////////////////////////////////////////////////////////////

        CompileResult CompileMnemonicType3(OpCode opCode, out byte[] outCode, out string errorMsg)
        {
            CompileResult result = CompileResult.Ok;
            errorMsg = string.Empty;
            Token token = new Token();

            lex.GetToken(ref token);

            switch (token.TokenType)
            {
                case Token.TokType.Int:
                    if (0 <= token.IntValue && token.IntValue <= 9)
                    {
                        outCode = new byte[2];
                        outCode[0] = (byte)(opCode.Function & 0xff);
                        outCode[1] = (byte)(token.IntValue & 0xff);
                    }
                    else
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                    }
                    break;

                case Token.TokType.Indirect:
                    lex.GetToken(ref token);

                    switch (token.TokenType)
                    {
                        case Token.TokType.Int:
                            if (0 <= token.IntValue && token.IntValue <= 101)
                            {
                                outCode = new byte[2];
                                outCode[0] = (byte)(opCode.IndirectFunction & 0xff);
                                outCode[1] = (byte)(token.IntValue | (byte)0x80);
                            }
                            else
                            {
                                outCode = null;
                                result = CompileResult.CompileError;
                                errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                            }
                            break;

                        case Token.TokType.Letter:
                        {
                            short Value;

                            if (parameter.GetStackParameter(token.StringValue, out Value))
                            {
                                outCode = new byte[2];
                                outCode[0] = (byte)(opCode.IndirectFunction & 0xff);
                                outCode[1] = (byte)(Value | (byte)0x80);
                            }
                            else
                            {
                                outCode = null;
                                result = CompileResult.CompileError;
                                errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                            }
                            break;
                        }

                        default:
                            outCode = null;
                            result = CompileResult.CompileError;
                            errorMsg = string.Format("Wrong parameter type or parameter expected");
                            break;
                    }
                    break;

                default:
                    outCode = null;
                    result = CompileResult.CompileError;
                    errorMsg = string.Format("Wrong parameter type or parameter out of range \"{0}\"", token.StringValue);
                    break;
            }

            return result;
        }

        /////////////////////////////////////////////////////////////

        CompileResult CompileMnemonicType4(OpCode opCode, out byte[] outCode, out string errorMsg)
        {
            CompileResult result = CompileResult.Ok;
            errorMsg = string.Empty;
            Token token = new Token();

            lex.GetToken(ref token);

            switch (token.TokenType)
            {
                case Token.TokType.Int:
                    if (0 <= token.IntValue && token.IntValue <= 55)
                    {
                        outCode = new byte[2];
                        outCode[0] = (byte)(opCode.Function & 0xff);
                        outCode[1] = (byte)(token.IntValue & 0xff);
                    }
                    else
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format("Wrong parameter type or parameter out of range \"{0}\"", token.StringValue);
                    }
                    break;

                case Token.TokType.Indirect:
                    lex.GetToken(ref token);

                    switch (token.TokenType)
                    {
                        case Token.TokType.Int:
                            if (0 <= token.IntValue && token.IntValue <= 101)
                            {
                                outCode = new byte[2];
                                outCode[0] = (byte)(opCode.IndirectFunction & 0xff);
                                outCode[1] = (byte)(token.IntValue | (byte)0x80);
                            }
                            else
                            {
                                outCode = null;
                                result = CompileResult.CompileError;
                                errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                            }
                            break;

                        case Token.TokType.Letter:
                        {
                            short Value;

                            if (parameter.GetStackParameter(token.StringValue, out Value))
                            {
                                outCode = new byte[2];
                                outCode[0] = (byte)(opCode.IndirectFunction & 0xff);
                                outCode[1] = (byte)(Value | (byte)0x80);
                            }
                            else
                            {
                                outCode = null;
                                result = CompileResult.CompileError;
                                errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                            }
                            break;
                        }

                        default:
                            outCode = null;
                            result = CompileResult.CompileError;
                            errorMsg = string.Format("Wrong parameter type or parameter expected");
                            break;
                    }
                    break;

                default:
                    outCode = null;
                    result = CompileResult.CompileError;
                    errorMsg = string.Format("Wrong parameter type or parameter out of range \"{0}\"", token.StringValue);
                    break;
            }

            return result;
        }

        /////////////////////////////////////////////////////////////

        CompileResult CompileMnemonicType5(OpCode opCode, out byte[] outCode, out string errorMsg)
        {
            CompileResult result = CompileResult.Ok;
            errorMsg = string.Empty;
            Token token = new Token();

            lex.GetToken(ref token);

            switch (token.TokenType)
            {
                case Token.TokType.Int:
                    if (opCode.ShortParamRange == FctType.R_0_14 && 0 <= token.IntValue && token.IntValue <= 14)
                    {
                        outCode = new byte[1];
                        outCode[0] = (byte)(((byte)opCode.ShortFunction + (byte)token.IntValue) & 0xff);
                    }
                    else
                    if (0 <= token.IntValue && token.IntValue <= 101)
                    {
                        outCode = new byte[2];
                        outCode[0] = (byte)(opCode.Function & 0xff);
                        outCode[1] = (byte)(token.IntValue & 0xff);
                    }
                    else
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                    }
                    break;

                case Token.TokType.Letter:
                {
                    short Value;

                    if (parameter.GetShortLabelParameter(token.StringValue, out Value))
                    {
                        outCode = new byte[2];
                        outCode[0] = (byte)(opCode.Function & 0xff);
                        outCode[1] = (byte)Value;
                    }
                    else
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                    }
                    break;
                }

                case Token.TokType.Text:
                    if (token.StringValue.Length <= 14)
                    {
                        outCode = new byte[token.StringValue.Length + 4];
                        outCode[0] = (byte)opCode.AlphaFunction;
                        outCode[1] = 0x00;
                        outCode[2] = (byte)(0xF1 + token.StringValue.Length);
                        outCode[3] = 0x00;

                        int outCodeLength = 4;

                        foreach (char c in token.StringValue)
                        {
                            outCode[outCodeLength++] = (byte)HP41CharacterConverter.ToHp41(c);
                        }
                    }
                    else
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                    }
                    break;

                default:
                    outCode = null;
                    result = CompileResult.CompileError;
                    errorMsg = string.Format("Wrong parameter type or parameter expected");
                    break;
            }

            return result;
        }

        /////////////////////////////////////////////////////////////

        CompileResult CompileMnemonicType6(OpCode opCode, out byte[] outCode, out string errorMsg)
        {
            CompileResult result = CompileResult.Ok;
            errorMsg = string.Empty;
            Token token = new Token();

            lex.GetToken(ref token);

            switch (token.TokenType)
            {
                case Token.TokType.Int:
                    if (opCode.ShortParamRange == FctType.R_0_14 && 0 <= token.IntValue && token.IntValue <= 14)
                    {
                        outCode = new byte[2];
                        outCode[0] = (byte)(((byte)opCode.ShortFunction + (byte)token.IntValue) & 0xff);
                        outCode[1] = 0x00;
                    }
                    else
                        if (0 <= token.IntValue && token.IntValue <= 101)
                    {
                        outCode = new byte[3];
                        outCode[0] = (byte)(opCode.Function & 0xff);
                        outCode[1] = (byte)0x00;
                        outCode[2] = (byte)(token.IntValue & 0xff);
                    }
                    else
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                    }
                    break;

                case Token.TokType.Letter:
                {
                    short Value;

                    if (parameter.GetShortLabelParameter(token.StringValue, out Value))
                    {
                        outCode = new byte[3];
                        outCode[0] = (byte)(opCode.Function & 0xff);
                        outCode[1] = (byte)0x00;
                        outCode[2] = (byte)Value;
                    }
                    else
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                    }
                    break;
                }

                case Token.TokType.Text:
                    if (token.StringValue.Length <= 14)
                    {
                        outCode = new byte[token.StringValue.Length + 2];
                        outCode[0] = (byte)opCode.AlphaFunction;
                        outCode[1] = (byte)(0xF0 + token.StringValue.Length);

                        int outCodeLength = 2;

                        foreach (char c in token.StringValue)
                        {
                            outCode[outCodeLength++] = (byte)HP41CharacterConverter.ToHp41(c);
                        }
                    }
                    else
                    {
                        outCode = null;
                        result = CompileResult.CompileError;
                        errorMsg = string.Format("String too long \"{0}\"", token.StringValue);
                    }
                    break;

                case Token.TokType.Indirect:
                    lex.GetToken(ref token);

                    switch (token.TokenType)
                    {
                        case Token.TokType.Int:
                            if (0 <= token.IntValue && token.IntValue <= 101)
                            {
                                outCode = new byte[2];
                                outCode[0] = (byte)(opCode.IndirectFunction & 0xff);
                                outCode[1] = (byte)(token.IntValue);

                                if (opCode.IndirectOr)
                                {
                                    outCode[1] |= (byte)0x80;
                                }
                            }
                            else
                            {
                                outCode = null;
                                result = CompileResult.CompileError;
                                errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                            }
                            break;

                        case Token.TokType.Letter:
                        {
                            short Value;

                            if (parameter.GetStackParameter(token.StringValue, out Value))
                            {
                                outCode = new byte[2];
                                outCode[0] = (byte)(opCode.IndirectFunction & 0xff);
                                outCode[1] = (byte)(Value);

                                if (opCode.IndirectOr)
                                {
                                    outCode[1] |= (byte)0x80;
                                }
                            }
                            else
                            {
                                outCode = null;
                                result = CompileResult.CompileError;
                                errorMsg = string.Format("Parameter out of range \"{0}\"", token.StringValue);
                            }
                            break;
                        }

                        default:
                            outCode = null;
                            result = CompileResult.CompileError;
                            errorMsg = string.Format("Wrong parameter type or parameter expected");
                            break;
                    }
                    break;

                default:
                    outCode = null;
                    result = CompileResult.CompileError;
                    errorMsg = string.Format("Wrong parameter type or parameter expected");
                    break;
            }

            return result;
        }

        /////////////////////////////////////////////////////////////

        CompileResult CompileMnemonicType7(OpCode opCode, out byte[] outCode, out string errorMsg)
        {
            CompileResult result = CompileResult.Ok;
            errorMsg = string.Empty;
            Token Token = new Token();

            int rom;

            lex.GetToken(ref Token);

            if (!(Token.TokenType == Token.TokType.Int && 0 <= Token.IntValue && Token.IntValue <= 31))
            {
                outCode = null;
                errorMsg = string.Format("Parameter 1 out of range \"{0}\"", Token.StringValue);
                return CompileResult.CompileError;
            }

            rom = Token.IntValue;
            lex.GetToken(ref Token);

            if (Token.TokenType != Token.TokType.Komma)
            {
                outCode = null;
                errorMsg = string.Format("',' expected instead of '{0}'", Token.StringValue);
                return CompileResult.CompileError;
            }

            lex.GetToken(ref Token);

            if (!(Token.TokenType == Token.TokType.Int && 0 <= Token.IntValue && Token.IntValue <= 63))
            {
                outCode = null;
                errorMsg = string.Format("Parameter 2 out of range \"{0}\"", Token.StringValue);
                return CompileResult.CompileError;
            }

            outCode = new byte[2];
            outCode[0] = (byte)(opCode.Function + ((byte)rom >> 2));
            outCode[1] = (byte)((((byte)rom & 0x03) << 6) + (byte)Token.IntValue);

            return result;
        }

        /////////////////////////////////////////////////////////////

        CompileResult CompileMnemonic(Token token, out byte[] outCode, out string errorMsg)
        {
            CompileResult result = CompileResult.Ok;
            errorMsg = string.Empty;
            OpCode opCode;

            if (!opCodes.FindMnemonic(token.StringValue, out opCode))
            {
                outCode = null;
                return CompileResult.UnknowStatement;
            }

            switch (opCode.FctType)
            {
                case FctType.NoParam:
                    outCode = new byte[1];
                    outCode[0] = (byte)(opCode.Function & 0xff);
                    break;

                case FctType.R_0_101_Stack:
                    result = CompileMnemonicType2(opCode, out outCode, out errorMsg);
                    break;

                case FctType.R_0_9:
                    result = CompileMnemonicType3(opCode, out outCode, out errorMsg);
                    break;

                case FctType.R_0_55:
                    result = CompileMnemonicType4(opCode, out outCode, out errorMsg);
                    break;

                case FctType.R_0_99_A_J_Alpha1:
                    result = CompileMnemonicType5(opCode, out outCode, out errorMsg);
                    break;

                case FctType.R_0_99_A_J_Alpha2:
                    result = CompileMnemonicType6(opCode, out outCode, out errorMsg);
                    break;

                case FctType.XRom:
                    result = CompileMnemonicType7(opCode, out outCode, out errorMsg);
                    break;

                default:
                    outCode = null;
                    break;
            }

            return result;
        }
    }
}
