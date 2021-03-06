﻿/* ........................................................................ *
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
    /// <summary>
    /// Bob Jenkin's Hash function
    /// see http://en.wikipedia.org/wiki/Jenkins_hash_function
    /// </summary>
    internal static class JenkinsHash
    {
        static void Mix(ref UInt32 a, ref UInt32 b, ref UInt32 c)
        {
            a -= b; a -= c; a ^= (c >> 13);
            b -= c; b -= a; b ^= (a << 8);
            c -= a; c -= b; c ^= (b >> 13);
            a -= b; a -= c; a ^= (c >> 12);
            b -= c; b -= a; b ^= (a << 16);
            c -= a; c -= b; c ^= (b >> 5);
            a -= b; a -= c; a ^= (c >> 3);
            b -= c; b -= a; b ^= (a << 10);
            c -= a; c -= b; c ^= (b >> 15);
        }

        /// <summary>
        /// Hash the vector K in hashes
        /// </summary>
        /// <param name="seed">Hash vector hash</param>
        /// <param name="k">Key to hash</param>
        /// <param name="hashes">Vector of 3 uints to set to the hash value</param>
        public static void HashVector(UInt32 seed, ReadOnlySpan<Byte> k, Span<UInt32> hashes)
        {
            var p = 0;
            var length = (UInt32)k.Length;
            var len = length;
            hashes[1] = 0x9e3779b9;
            hashes[0] = 0x9e3779b9;
            hashes[2] = seed;

            while (len >= 12)
            {
                hashes[0] += (k[p+0] + ((UInt32)k[p+1] << 8) + ((UInt32)k[p+2] << 16) + ((UInt32)k[p+3] << 24));
                hashes[1] += (k[p+4] + ((UInt32)k[p+5] << 8) + ((UInt32)k[p+6] << 16) + ((UInt32)k[p+7] << 24));
                hashes[2] += (k[p+8] + ((UInt32)k[p+9] << 8) + ((UInt32)k[p+10] << 16) + ((UInt32)k[p+11] << 24));
                Mix(ref hashes[0], ref hashes[1], ref hashes[2]);
                p += 12; len -= 12;
            }

            /*------------------------------------- handle the last 11 bytes */
            hashes[2] += length;
            switch (len)              /* all the case statements fall through */
            {
                case 11:    hashes[2] += ((UInt32)k[p + 10] << 24);   goto case 10;
                case 10:    hashes[2] += ((UInt32)k[p + 9] << 16);    goto case 9;
                case 9:     hashes[2] += ((UInt32)k[p + 8] << 8);     goto case 8;
                    /* the first byte of hashes[2] is reserved for the length */
                case 8:     hashes[1] += ((UInt32)k[p + 7] << 24);    goto case 7;
                case 7:     hashes[1] += ((UInt32)k[p + 6] << 16);    goto case 6;
                case 6:     hashes[1] += ((UInt32)k[p + 5] << 8);     goto case 5;
                case 5:     hashes[1] += (UInt32)k[p + 4];            goto case 4;
                case 4:     hashes[0] += ((UInt32)k[p + 3] << 24);    goto case 3;
                case 3:     hashes[0] += ((UInt32)k[p + 2] << 16);    goto case 2;
                case 2:     hashes[0] += ((UInt32)k[p + 1] << 8);     goto case 1;
                case 1:     hashes[0] += (UInt32)k[p + 0];            break;
                    /* case 0: nothing left to add */
            }

            Mix(ref hashes[0], ref hashes[1], ref hashes[2]);
        }

    }
}