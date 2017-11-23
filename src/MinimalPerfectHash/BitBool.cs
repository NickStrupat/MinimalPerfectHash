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

using System;

namespace MinimalPerfectHash
{
    internal static class BitBool
    {
        public static void SetBitsAtPos(UInt32[] bitsTable, UInt32 pos, UInt32 bitsString, UInt32 stringLength)
        {
            var wordIdx = pos >> 5;
            var shift1 = pos & 0x1f;
            var shift2 = 0x20 - shift1;
            var stringMask = (UInt32) ((1 << (Int32)stringLength) - 1);
            bitsTable[wordIdx] &= ~(stringMask << (Int32)shift1);
            bitsTable[wordIdx] |= bitsString << (Int32)shift1;
            if (shift2 < stringLength)
            {
                bitsTable[wordIdx + 1] &= ~(stringMask >> (Int32)shift2);
                bitsTable[wordIdx + 1] |= bitsString >> (Int32)shift2;
            }
        }

        public static UInt32 GetBitsAtPos(UInt32[] bitsTable, UInt32 pos, UInt32 stringLength)
        {
            var wordIdx = pos >> 5;
            var shift1 = pos & 0x1f;
            var shift2 = 0x20 - shift1;
            var stringMask = (UInt32)((1 << (Int32)stringLength) - 1);
            var bitsString = (bitsTable[wordIdx] >> (Int32)shift1) & stringMask;
            if (shift2 < stringLength)
            {
                bitsString |= (bitsTable[(Int32)(wordIdx + 1)] << (Int32)shift2) & stringMask;
            }
            return bitsString;
        }

        public static void SetBitsValue(UInt32[] bitsTable, UInt32 index, UInt32 bitsString, UInt32 stringLength, UInt32 stringMask)
        {
            var bitIdx = index * stringLength;
            var wordIdx = bitIdx >> 5;
            var shift1 = bitIdx & 0x1f;
            var shift2 = 0x20 - shift1;
            bitsTable[wordIdx] &= ~(stringMask << (Int32)shift1);
            bitsTable[wordIdx] |= bitsString << (Int32)shift1;
            if (shift2 < stringLength)
            {
                bitsTable[wordIdx + 1] &= ~(stringMask >> (Int32)shift2);
                bitsTable[wordIdx + 1] |= bitsString >> (Int32)shift2;
            }
        }

        public static UInt32 GetBitsValue(UInt32[] bitsTable, UInt32 index, UInt32 stringLength, UInt32 stringMask)
        {
            var bitIdx = index * stringLength;
            var wordIdx = bitIdx >> 5;
            var shift1 = bitIdx & 0x1f;
            var shift2 = 0x20 - shift1;
            var bitsString = (bitsTable[wordIdx] >> (Int32)shift1) & stringMask;
            if (shift2 < stringLength)
            {
                bitsString |= (bitsTable[(Int32)(wordIdx + 1)] << (Int32)shift2) & stringMask;
            }
            return bitsString;
        }

 


    }
}