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
    [Serializable]
    internal class CompressedSeq
    {
        UInt32[] _lengthRems;
        UInt32 _n;
        UInt32 _remR;
        Select _sel;
        UInt32[] _storeTable;
        UInt32 _totalLength;


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

            _n = n;
            _totalLength = 0;

            for (i = 0; i < _n; i++)
            {
                if (valsTable[i] == 0)
                {
                    lengths[i] = 0;
                }
                else
                {
                    lengths[i] = ILog2(valsTable[i] + 1);
                    _totalLength += lengths[i];
                }
            }

            _storeTable = new UInt32[(_totalLength + 31) >> 5];
            _totalLength = 0;

            for (i = 0; i < _n; i++)
            {
                if (valsTable[i] == 0)
                    continue;
                var storedValue = valsTable[i] - ((1U << (Int32)lengths[i]) - 1U);
                BitBool.SetBitsAtPos(_storeTable, _totalLength, storedValue, lengths[i]);
                _totalLength += lengths[i];
            }

            _remR = ILog2(_totalLength / _n);

            if (_remR == 0)
            {
                _remR = 1;
            }

            _lengthRems = new UInt32[((_n * _remR) + 0x1f) >> 5];

            var remsMask = (1U << (Int32)_remR) - 1U;
            _totalLength = 0;

            for (i = 0; i < _n; i++)
            {
                _totalLength += lengths[i];
                BitBool.SetBitsValue(_lengthRems, i, _totalLength & remsMask, _remR, remsMask);
                lengths[i] = _totalLength >> (Int32)_remR;
            }

            _sel = new Select();

            _sel.Generate(lengths, _n, (_totalLength >> (Int32)_remR));
        }

        public UInt32 Query(UInt32 idx)
        {
            UInt32 selRes;
            UInt32 encIdx;
            var remsMask = (UInt32)((1 << (Int32)_remR) - 1);

            if (idx == 0)
            {
                encIdx = 0;
                selRes = _sel.Query(idx);
            }
            else
            {
                selRes = _sel.Query(idx - 1);
                encIdx = (selRes - (idx - 1)) << (Int32)_remR;
                encIdx += BitBool.GetBitsValue(_lengthRems, idx - 1, _remR, remsMask);
                selRes = _sel.NextQuery(selRes);
            }
            var encLength = (selRes - idx) << (Int32)_remR;
            encLength += BitBool.GetBitsValue(_lengthRems, idx, _remR, remsMask);
            encLength -= encIdx;
            if (encLength == 0)
            {
                return 0;
            }
            return (BitBool.GetBitsAtPos(_storeTable, encIdx, encLength) + ((UInt32)((1 << (Int32)encLength) - 1)));
        }
    }
}