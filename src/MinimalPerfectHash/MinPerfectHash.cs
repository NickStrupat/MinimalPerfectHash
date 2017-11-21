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
    /// <summary>
    /// Minimum Perfect Hash function class
    /// </summary>
    [Serializable]
    public class MinPerfectHash
    {
        CompressedSeq _cs;
        uint _hashSeed, _n, _nbuckets;

        /// <summary>
        /// Create a minimum perfect hash function for the provided key set
        /// </summary>
        /// <param name="keySource">Key source</param>
        /// <param name="c">Load factor (.5 &gt; c &gt; .99)</param>
        /// <returns>Created Minimum Perfect Hash function</returns>
        public static MinPerfectHash Create(IKeySource keySource, double c)
        {
            var buckets = new Buckets(keySource, c);
            var dispTable = new uint[buckets.NBuckets];
            uint hashSeed;

            var iteration = 100;
            for (; ; iteration--)
            {
                uint maxBucketSize;
                if (!buckets.MappingPhase(out hashSeed, out maxBucketSize))
                {
                    throw new Exception("Mapping failure. Duplicate keys?");
                }

                var sortedLists = buckets.OrderingPhase(maxBucketSize);
                var searchingSuccess = buckets.SearchingPhase(maxBucketSize, sortedLists, dispTable);

                if (searchingSuccess) break;

                if (iteration <= 0)
                {
                    throw new Exception("Too many iteration");
                }
            }

            var cs = new CompressedSeq();
            cs.Generate(dispTable, (uint)dispTable.Length);

            var ret = new MinPerfectHash { _hashSeed = hashSeed, _cs = cs, _nbuckets = buckets.NBuckets, _n = buckets.N };

            return ret;
        }

        /// <summary>
        /// Maximun value of the hash function.
        /// </summary>
        public uint N { get { return _n; }}

        /// <summary>
        /// Compute the hash value associate with the key
        /// </summary>
        /// <param name="key">key from the original key set</param>
        /// <returns>Hash value (0 &gt; hash &gt; N)</returns>
        public uint Search(byte[] key)
        {
            var hl = new uint[3];
            JenkinsHash.HashVector(_hashSeed, key, hl);
            var g = hl[0] % _nbuckets;
            var f = hl[1] % _n;
            var h = hl[2] % (_n - 1) + 1;

            var disp = _cs.Query(g);
            var probe0Num = disp % _n;
            var probe1Num = disp / _n;
            var position = (uint)((f + ((ulong)h) * probe0Num + probe1Num) % _n);
            return position;
        }
    }

}