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
using System.Text.RegularExpressions;

namespace FocalCompiler
{
    class Token
    {
        public enum TokType
        {
            Id,
            Int,
            Letter,
            Indirect,
            Komma,
            Append,
            Number,
            Text,
            Comment,
            Eol
        }

        public TokType TokenType;
        public short IntValue;
        public string StringValue;

        public Token ()
        {
        }

        public Token (Token Token2)
        {
            TokenType = Token2.TokenType;
            IntValue = Token2.IntValue;
            StringValue = Token2.StringValue;
        }
    }

    //////////////////////////////////////////////////////////////////

    class Lex
    {
        private Dictionary<String, Token> defines = new Dictionary<string, Token> ();

        private Regex parser;
        private Match match;

        /////////////////////////////////////////////////////////////

        bool GetNextToken (ref Token token)
        {
            const int RegexIdxAll        = 0;
            const int RegexIdxQuotedText = 1;
            const int RegexIdxText       = 2;
            const int RegexIdxNumber     = 4;
            const int RegexIdxExponent   = 7;
            const int RegexIdxSingleExponent = 8;
            const int RegexIdxIndirect   = 9;
            const int RegexIdxLetter     = 10;
            const int RegexIdxComment    = 11;
            const int RegexIdxKomma      = 12;
            const int RegexIdxAppend     = 13;
            const int RegexIdxId         = 14;

            if (match.Groups[RegexIdxIndirect].Success)
            {
                token.TokenType = Token.TokType.Indirect;
                return true;
            }

            if (match.Groups[RegexIdxNumber].Success)
            {
                if (match.Groups[RegexIdxExponent].Success || match.Groups[RegexIdxSingleExponent].Success)
                {
                    token.TokenType = Token.TokType.Number;
                    token.StringValue = match.Groups[RegexIdxNumber].Value;
                }
                else
                {
                    token.TokenType = Token.TokType.Int;
                    Int16.TryParse (match.Groups[RegexIdxNumber].Value, out token.IntValue);
                    token.StringValue = match.Groups[RegexIdxNumber].Value;
                }

                return true;
            }

            if (match.Groups[RegexIdxLetter].Success)
            {
                token.TokenType = Token.TokType.Letter;
                token.StringValue = match.Groups[RegexIdxLetter].Value;
                return true;
            }

            if (match.Groups[RegexIdxKomma].Success)
            {
                token.TokenType = Token.TokType.Komma;
                return true;
            }

            if (match.Groups[RegexIdxAppend].Success)
            {
                token.TokenType = Token.TokType.Append;
                return true;
            }

            if (match.Groups[RegexIdxQuotedText].Success)
            {
                token.TokenType = Token.TokType.Text;
                token.StringValue = match.Groups[RegexIdxText].Value;
                return true;
            }

            if (match.Groups[RegexIdxId].Success)
            {
                token.TokenType = Token.TokType.Id;
                token.StringValue = match.Groups[RegexIdxId].Value;

                Token DefineToken;

                if (defines.TryGetValue (token.StringValue, out DefineToken))
                {
                    token.TokenType = DefineToken.TokenType;
                    token.StringValue = DefineToken.StringValue;
                    token.IntValue = DefineToken.IntValue;
                }
                return true;
            }

            if (match.Groups[RegexIdxComment].Success)
            {
                token.TokenType = Token.TokType.Comment;
                token.StringValue = match.Groups[RegexIdxComment].Value;
                return true;
            }

            token.TokenType = Token.TokType.Eol;
            token.StringValue = match.Groups[RegexIdxAll].Value;
            return false;
        }

        /////////////////////////////////////////////////////////////

        public bool GetFirstToken (string line, ref Token token)
        {
            if (parser == null)
            {
                //1:$[1] # 2:$[2] # 3:$[3] # 4:$[4] # 5:$[5] # 6:$[6] # 7:$[7] # 8:$[8] # 9:$[9] # 10:$[10] # 11:$[11] # 12:$[12]
                parser = new Regex (@"(""(.+)"")|(((-?\d*\.?\d+((e)-?\d+)?)|(e-?\d+))(?=[\t ,]|$))|(ind)|([A-Z](?=[\t ,]|$))|(;.*$)|(,)|(>)|([^, \t]+)", RegexOptions.IgnoreCase);
            }

            match = parser.Match (line);

            if (match.Success)
            {
                return GetNextToken (ref token);
            }

            token.TokenType = Token.TokType.Eol;
            token.StringValue = match.Groups[0].Value;
            return false;
        }

        /////////////////////////////////////////////////////////////

        public bool GetToken (ref Token token)
        {
            match = match.NextMatch ();

            if (match.Success)
            {
                return GetNextToken (ref token);
            }

            token.TokenType = Token.TokType.Eol;
            token.StringValue = match.Groups[0].Value;
            return false;
        }

        /////////////////////////////////////////////////////////////

        public void AddDefine (string define, Token token)
        {
            defines.Add (define, new Token (token));
        }
    }
}
