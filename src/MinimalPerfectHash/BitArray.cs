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

        public bool GetBit(ulong i) 
        {
            fixed (byte* ptrTable = &_table[0])
            {
                var i32OccupTable = (uint*) ptrTable;
	            return (i32OccupTable[i >> 5] & (1u << ((int)i & 0x0000001f)))!=0;
            }
        }

        public void SetBit(uint i)
        {
            fixed (byte* ptrTable = &_table[0])
            {
                var i32OccupTable = (uint*) ptrTable;
                i32OccupTable[i >> 5] |= 1u << ((int)i & 0x0000001f);
            }
        }

        public void UnSetBit(uint i)
        {
            fixed (byte* ptrTable = &_table[0])
            {
                var i32OccupTable = (uint*) ptrTable;
                i32OccupTable[i >> 5] ^= 1u << ((int)i & 0x0000001f);
            }
        }

        public void Zero()
        {
            Array.Clear(_table, 0, _table.Length);
        }
    }
}