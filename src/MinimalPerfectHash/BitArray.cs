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

namespace MPHTest.MPH
{
    unsafe internal class BitArray
    {
        static readonly uint[] Bitmask32 = new uint[] { 
              1, 2, 4, 8, 0x10, 0x20, 0x40, 0x80, 0x100, 0x200, 0x400, 0x800, 0x1000, 0x2000, 0x4000, 0x8000, 
              0x10000, 0x20000, 0x40000, 0x80000, 0x100000, 0x200000, 0x400000, 0x800000, 0x1000000, 0x2000000, 
              0x4000000, 0x8000000, 0x10000000, 0x20000000, 0x40000000, 0x80000000 };

        readonly byte[] _table;

        public BitArray(int size)
        {
            _table = new byte[size];
        }

        public byte this[int i]
        {
            get { return _table[i]; }
            set { _table[i] = value; }
        }

        public bool GetBit(uint i) 
        {
            fixed (byte* ptrTable = &_table[0])
            {
                var i32OccupTable = (uint*) ptrTable;
                return (i32OccupTable[i >> 5] & Bitmask32[i & 0x0000001f])!=0;
            }
        }

        public void SetBit(uint i)
        {
            fixed (byte* ptrTable = &_table[0])
            {
                var i32OccupTable = (uint*) ptrTable;
                i32OccupTable[i >> 5] |= Bitmask32[i & 0x0000001f];
            }
        }

        public void UnSetBit(uint i)
        {
            fixed (byte* ptrTable = &_table[0])
            {
                var i32OccupTable = (uint*) ptrTable;
                i32OccupTable[i >> 5] ^= ((Bitmask32[i & 0x0000001f]));
            }
        }

        public void Zero()
        {
            Array.Clear(_table, 0, _table.Length);
        }
    }
}