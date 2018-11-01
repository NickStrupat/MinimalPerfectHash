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
using System.Collections.Generic;

namespace MinimalPerfectHash
{
    [Serializable]
    internal class CompressedSeq
    {
        UInt32[] lengthRems;
        UInt32 n;
        UInt32 remR;
        Select sel;
        UInt32[] storeTable;
        UInt32 totalLength;

		public CompressedSeq() { }

		internal Int32 Size => (sizeof(UInt32) * (5 + lengthRems.Length + storeTable.Length)) + sel.Size;

		internal void Dump(Span<UInt32> span)
		{
			var i = 0;
			var lengthRemsLength = (UInt32) lengthRems.Length;
			span[i++] = lengthRemsLength;
			for (var j = 0; j != lengthRemsLength; j++)
				span[i++] = lengthRems[j];

			span[i++] = n;
			span[i++] = remR;

			sel.Dump(span.Slice(i));
			i += sel.Size / sizeof(UInt32);

			var storeTableLength = (UInt32) storeTable.Length;
			span[i++] = storeTableLength;
			for (var j = 0; j != storeTableLength; j++)
				span[i++] = storeTable[j];

			span[i++] = totalLength;
		}

		internal CompressedSeq(ReadOnlySpan<UInt32> span)
		{
			var i = 0;
			var lengthRemsLength = span[i++];
			lengthRems = new UInt32[lengthRemsLength];
			for (var j = 0; j != lengthRemsLength; j++)
				lengthRems[j] = span[i++];

			n = span[i++];
			remR = span[i++];

			sel = new Select(span.Slice(i));
			i += sel.Size / sizeof(UInt32);

			var storeTableLength = span[i++];
			storeTable = new UInt32[storeTableLength];
			for (var j = 0; j != storeTableLength; j++)
				storeTable[j] = span[i++];

			totalLength = span[i++];
		}

		static UInt32 ILog2(UInt32 x)
        {
            UInt32 res = 0;

            while (x > 1)
            {
                x >>= 1;
                res++;
            }
            return res;
        }

        public void Generate(UInt32[] valsTable, UInt32 n)
        {
            UInt32 i;
            // lengths: represents lengths of encoded values	
            var lengths = new UInt32[n];

            this.n = n;
            totalLength = 0;

            for (i = 0; i < this.n; i++)
            {
                if (valsTable[i] == 0)
                {
                    lengths[i] = 0;
                }
                else
                {
                    lengths[i] = ILog2(valsTable[i] + 1);
                    totalLength += lengths[i];
                }
            }

            storeTable = new UInt32[(totalLength + 31) >> 5];
            totalLength = 0;

            for (i = 0; i < this.n; i++)
            {
                if (valsTable[i] == 0)
                    continue;
                var storedValue = valsTable[i] - ((1U << (Int32)lengths[i]) - 1U);
                BitBool.SetBitsAtPos(storeTable, totalLength, storedValue, lengths[i]);
                totalLength += lengths[i];
            }

            remR = ILog2(totalLength / this.n);

            if (remR == 0)
            {
                remR = 1;
            }

            lengthRems = new UInt32[((this.n * remR) + 0x1f) >> 5];

            var remsMask = (1U << (Int32)remR) - 1U;
            totalLength = 0;

            for (i = 0; i < this.n; i++)
            {
                totalLength += lengths[i];
                BitBool.SetBitsValue(lengthRems, i, totalLength & remsMask, remR, remsMask);
                lengths[i] = totalLength >> (Int32)remR;
            }

            sel = new Select();

            sel.Generate(lengths, this.n, (totalLength >> (Int32)remR));
        }

        public UInt32 Query(UInt32 idx)
        {
            UInt32 selRes;
            UInt32 encIdx;
            var remsMask = (UInt32)((1 << (Int32)remR) - 1);

            if (idx == 0)
            {
                encIdx = 0;
                selRes = sel.Query(idx);
            }
            else
            {
                selRes = sel.Query(idx - 1);
                encIdx = (selRes - (idx - 1)) << (Int32)remR;
                encIdx += BitBool.GetBitsValue(lengthRems, idx - 1, remR, remsMask);
                selRes = sel.NextQuery(selRes);
            }
            var encLength = (selRes - idx) << (Int32)remR;
            encLength += BitBool.GetBitsValue(lengthRems, idx, remR, remsMask);
            encLength -= encIdx;
            if (encLength == 0)
            {
                return 0;
            }
            return (BitBool.GetBitsAtPos(storeTable, encIdx, encLength) + ((UInt32)((1 << (Int32)encLength) - 1)));
        }
    }
}