/* ........................................................................ *
 * (c) 2010 Laurent Dupuis (www.dupuis.me)                                  *
 * ........................................................................ *
 * < This program is free software: you can redistribute it and/or modify
 * < it under the terms of the GNU General Public License as published by
 * < the Free Software Foundation, either version 3 of the License, or
 * < (at your option) any later version.
 * < 
 * < This program is distributed in the hope that it will be useful,
 * < but WITHOUT ANY WARRANTY; without even the implied warranty of
 * < MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * < GNU General Public License for more details.
 * < 
 * < You should have received a copy of the GNU General Public License
 * < along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * ........................................................................ */

namespace MPHTest.MPH
{
    internal static class BitBool
    {
        public static void SetBitsAtPos(uint[] bitsTable, uint pos, uint bitsString, uint stringLength)
        {
            var wordIdx = pos >> 5;
            var shift1 = pos & 0x1f;
            var shift2 = 0x20 - shift1;
            var stringMask = (uint) ((1 << (int)stringLength) - 1);
            bitsTable[wordIdx] &= ~(stringMask << (int)shift1);
            bitsTable[wordIdx] |= bitsString << (int)shift1;
            if (shift2 < stringLength)
            {
                bitsTable[wordIdx + 1] &= ~(stringMask >> (int)shift2);
                bitsTable[wordIdx + 1] |= bitsString >> (int)shift2;
            }
        }

        public static uint GetBitsAtPos(uint[] bitsTable, uint pos, uint stringLength)
        {
            var wordIdx = pos >> 5;
            var shift1 = pos & 0x1f;
            var shift2 = 0x20 - shift1;
            var stringMask = (uint)((1 << (int)stringLength) - 1);
            var bitsString = (bitsTable[wordIdx] >> (int)shift1) & stringMask;
            if (shift2 < stringLength)
            {
                bitsString |= (bitsTable[(int)(wordIdx + 1)] << (int)shift2) & stringMask;
            }
            return bitsString;
        }

        public static void SetBitsValue(uint[] bitsTable, uint index, uint bitsString, uint stringLength, uint stringMask)
        {
            var bitIdx = index * stringLength;
            var wordIdx = bitIdx >> 5;
            var shift1 = bitIdx & 0x1f;
            var shift2 = 0x20 - shift1;
            bitsTable[wordIdx] &= ~(stringMask << (int)shift1);
            bitsTable[wordIdx] |= bitsString << (int)shift1;
            if (shift2 < stringLength)
            {
                bitsTable[wordIdx + 1] &= ~(stringMask >> (int)shift2);
                bitsTable[wordIdx + 1] |= bitsString >> (int)shift2;
            }
        }

        public static uint GetBitsValue(uint[] bitsTable, uint index, uint stringLength, uint stringMask)
        {
            var bitIdx = index * stringLength;
            var wordIdx = bitIdx >> 5;
            var shift1 = bitIdx & 0x1f;
            var shift2 = 0x20 - shift1;
            var bitsString = (bitsTable[wordIdx] >> (int)shift1) & stringMask;
            if (shift2 < stringLength)
            {
                bitsString |= (bitsTable[(int)(wordIdx + 1)] << (int)shift2) & stringMask;
            }
            return bitsString;
        }

 


    }
}