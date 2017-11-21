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
        uint[] _lengthRems;
        uint _n;
        uint _remR;
        Select _sel;
        uint[] _storeTable;
        uint _totalLength;


        static uint ILog2(uint x)
        {
            uint res = 0;

            while (x > 1)
            {
                x >>= 1;
                res++;
            }
            return res;
        }

        public void Generate(uint[] valsTable, uint n)
        {
            uint i;
            // lengths: represents lengths of encoded values	
            var lengths = new uint[n];

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

            _storeTable = new uint[(_totalLength + 31) >> 5];
            _totalLength = 0;

            for (i = 0; i < _n; i++)
            {
                if (valsTable[i] == 0)
                    continue;
                var storedValue = valsTable[i] - ((1U << (int)lengths[i]) - 1U);
                BitBool.SetBitsAtPos(_storeTable, _totalLength, storedValue, lengths[i]);
                _totalLength += lengths[i];
            }

            _remR = ILog2(_totalLength / _n);

            if (_remR == 0)
            {
                _remR = 1;
            }

            _lengthRems = new uint[((_n * _remR) + 0x1f) >> 5];

            var remsMask = (1U << (int)_remR) - 1U;
            _totalLength = 0;

            for (i = 0; i < _n; i++)
            {
                _totalLength += lengths[i];
                BitBool.SetBitsValue(_lengthRems, i, _totalLength & remsMask, _remR, remsMask);
                lengths[i] = _totalLength >> (int)_remR;
            }

            _sel = new Select();

            _sel.Generate(lengths, _n, (_totalLength >> (int)_remR));
        }

        public uint Query(uint idx)
        {
            uint selRes;
            uint encIdx;
            var remsMask = (uint)((1 << (int)_remR) - 1);

            if (idx == 0)
            {
                encIdx = 0;
                selRes = _sel.Query(idx);
            }
            else
            {
                selRes = _sel.Query(idx - 1);
                encIdx = (selRes - (idx - 1)) << (int)_remR;
                encIdx += BitBool.GetBitsValue(_lengthRems, idx - 1, _remR, remsMask);
                selRes = _sel.NextQuery(selRes);
            }
            var encLength = (selRes - idx) << (int)_remR;
            encLength += BitBool.GetBitsValue(_lengthRems, idx, _remR, remsMask);
            encLength -= encIdx;
            if (encLength == 0)
            {
                return 0;
            }
            return (BitBool.GetBitsAtPos(_storeTable, encIdx, encLength) + ((uint)((1 << (int)encLength) - 1)));
        }
    }
}