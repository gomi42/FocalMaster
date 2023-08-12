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

using System.Collections.Generic;

namespace FocalMaster.Helper
{
    static class HP41CharacterConverter
    {
        static private Dictionary<char, int> encoderTable;

        static private Dictionary<int, char> decoderTable = new Dictionary<int, char>()
        {
            {1, 'ˣ'},
            {2, 'ẍ'},
            {3, '←'},
            {4, 'α'},
            {5, 'β'},
            {6, 'Γ'},
            {7, '↓'},
            {8, 'Δ'},
            {9, 'σ'},
            {10, '♦'},
            {11, 'λ'},
            {12, 'μ'},
            {13, '@'}, // according to the HP document "Creating Your Own HP-41 Bar Code" page 17
            {14, 'τ'},
            {15, 'Φ'},
            {16, 'Θ'},
            {17, 'Ω'},
            {18, 'δ'},
            {19, 'Ȧ'},
            {20, 'ȧ'},
            {21, 'Ä'},
            {22, 'ä'},
            {23, 'Ö'},
            {24, 'ö'},
            {25, 'Ü'},
            {26, 'ü'},
            {27, 'Æ'},
            {28, 'œ'},
            {29, '#'}, // according to the HP document "Creating Your Own HP-41 Bar Code" page 17
            {30, '£'},
            {31, '▒'},

            {96, '┬'},

            {123, 'π'},
            {124, '|'},
            {125, '→'},
            {126, '&'}, // according to the HP document "Creating Your Own HP-41 Bar Code" page 17
            {127, 'Ⱶ'},
        };

        /////////////////////////////////////////////////////////////

        static HP41CharacterConverter()
        {
            encoderTable = new Dictionary<char, int>();

            foreach (var kpv in decoderTable)
            {
                encoderTable[kpv.Value] = kpv.Key;
            }
        }

        /////////////////////////////////////////////////////////////

        public static char FromHp41(int character)
        {
            if (decoderTable.TryGetValue(character, out char decoded))
            {
                return decoded;
            }

            return (char)character;
        }

        /////////////////////////////////////////////////////////////

        public static int ToHp41(char character)
        {
            if (encoderTable.TryGetValue(character, out int encoded))
            {
                return encoded;
            }

            return character;
        }
    }
}
