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
        CompressedSeq cs;
        UInt32 hashSeed, n, nbuckets;

        /// <summary>
        /// Create a minimum perfect hash function for the provided key set
        /// </summary>
        /// <param name="keySource">Key source</param>
        /// <param name="c">Load factor (.5 &gt; c &gt; .99)</param>
        /// <returns>Created Minimum Perfect Hash function</returns>
        public static MinPerfectHash Create(IKeySource keySource, Double c)
        {
            var buckets = new Buckets(keySource, c);
            var dispTable = new UInt32[buckets.NBuckets];
            UInt32 hashSeed;

            var iteration = 100;
            for (; ; iteration--)
            {
                UInt32 maxBucketSize;
                if (!buckets.MappingPhase(out hashSeed, out maxBucketSize))
                {
                    throw new Exception("Mapping failure. Duplicate keys?");
                }

                var sortedLists = buckets.OrderingPhase(maxBucketSize);
                var searchingSuccess = buckets.SearchingPhase(maxBucketSize, sortedLists, dispTable);

                if (searchingSuccess)
					break;

                if (iteration <= 0)
                {
                    throw new Exception("Too many iteration");
                }
            }

            var cs = new CompressedSeq();
            cs.Generate(dispTable, (UInt32)dispTable.Length);

            var ret = new MinPerfectHash { hashSeed = hashSeed, cs = cs, nbuckets = buckets.NBuckets, n = buckets.N };

            return ret;
        }

        /// <summary>
        /// Maximun value of the hash function.
        /// </summary>
        public UInt32 N => n;

	    /// <summary>
        /// Compute the hash value associate with the key
        /// </summary>
        /// <param name="key">key from the original key set</param>
        /// <returns>Hash value (0 &gt; hash &gt; N)</returns>
        public UInt32 Search(Byte[] key)
        {
            var hl = new UInt32[3];
            JenkinsHash.HashVector(hashSeed, key, hl);
            var g = hl[0] % nbuckets;
            var f = hl[1] % n;
            var h = hl[2] % (n - 1) + 1;

            var disp = cs.Query(g);
            var probe0Num = disp % n;
            var probe1Num = disp / n;
            var position = (UInt32)((f + ((UInt64)h) * probe0Num + probe1Num) % n);
            return position;
        }
    }

}