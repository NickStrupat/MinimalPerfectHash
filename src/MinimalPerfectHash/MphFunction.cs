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
using System.Runtime.InteropServices;

namespace MinimalPerfectHash
{
	/// <summary>
	/// Minimum Perfect Hash function class
	/// </summary>
	[Serializable]
    public class MphFunction
    {
        private readonly CompressedSeq cs;
        private readonly UInt32 hashSeed;
	    private readonly UInt32 maxValue;
	    private readonly UInt32 nBuckets;

	    /// <summary>
	    /// Maximun value of the hash function.
	    /// </summary>
	    public UInt32 MaxValue => maxValue;

		private MphFunction(CompressedSeq cs, UInt32 hashSeed, UInt32 maxValue, UInt32 nBuckets)
		{
			this.cs = cs;
			this.hashSeed = hashSeed;
			this.maxValue = maxValue;
			this.nBuckets = nBuckets;
		}

		private protected MphFunction() { }

		/// <summary>
		/// Create a minimum perfect hash function for the provided key set
		/// </summary>
		/// <param name="loadFactor">Load factor (.5 &gt; c &gt; .99)</param>
		public MphFunction(IList<Byte[]> keys, Double loadFactor) : this(keys, (UInt32) keys.Count, loadFactor) {}

		/// <summary>
		/// Create a minimum perfect hash function for the provided key set
		/// </summary>
		/// <param name="keySource">Key source</param>
		/// <param name="keyCount">Key count</param>
		/// <param name="loadFactor">Load factor (.5 &gt; c &gt; .99)</param>
		public MphFunction(IEnumerable<Byte[]> keySource, UInt32 keyCount, Double loadFactor)
		{
			var buckets = new Buckets(keySource, keyCount, loadFactor);
			var dispTable = new UInt32[buckets.BucketCount];

			var iteration = 100;
			for (; ; iteration--)
			{
				if (!buckets.MappingPhase(out hashSeed, out var maxBucketSize))
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

			cs = new CompressedSeq();
			cs.Generate(dispTable, (UInt32)dispTable.Length);
			nBuckets = buckets.BucketCount;
			maxValue = buckets.BinCount;
		}

	    /// <summary>
        /// Compute the hash value associate with the key
        /// </summary>
        /// <param name="key">key from the original key set</param>
        /// <returns>Hash value (0 &gt; hash &gt; N)</returns>
        public UInt32 GetHash(ReadOnlySpan<Byte> key)
        {
			Span<UInt32> hl = stackalloc UInt32[3];
            JenkinsHash.HashVector(hashSeed, key, hl);
            var g = hl[0] % nBuckets;
            var f = hl[1] % maxValue;
            var h = hl[2] % (maxValue - 1) + 1;

            var disp = cs.Query(g);
            var probe0Num = disp % maxValue;
            var probe1Num = disp / maxValue;
            var position = (UInt32)((f + ((UInt64)h) * probe0Num + probe1Num) % maxValue);
            return position;
		}

		public Byte[] Dump()
		{
			var size = (sizeof(UInt32) * 3) + cs.Size;
			var bytes = new Byte[size];
			var span = MemoryMarshal.Cast<Byte, UInt32>(new Span<Byte>(bytes));
			var i = 0;
			span[i++] = hashSeed;
			span[i++] = maxValue;
			span[i++] = nBuckets;
			cs.Dump(span.Slice(i));
			return bytes;
		}

		public static MphFunction Load(Byte[] bytes)
		{
			var span = MemoryMarshal.Cast<Byte, UInt32>(new ReadOnlySpan<Byte>(bytes));
			var i = 0;
			UInt32 hashSeed = span[i++];
			UInt32 maxValue = span[i++];
			UInt32 nBuckets = span[i++];
			CompressedSeq cs = new CompressedSeq(span.Slice(i));
			return new MphFunction(cs, hashSeed, maxValue, nBuckets);
		}
	}
}