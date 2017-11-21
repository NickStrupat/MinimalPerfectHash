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
    /// Miller–Rabin primality test
    /// </summary>
    internal static class MillerRabin
    {
        private static UInt64 IntPow(UInt64 a, UInt64 d, UInt64 n)
        {
            UInt64 aPow = a;
            UInt64 res = 1L;
            while (d > 0L)
            {
                if ((d & 1L) == 1L)
                {
                    res = (res * aPow) % n;
                }
                aPow = (aPow * aPow) % n;
                d /= 2L;
            }
            return res;
        }

        private static Boolean CheckWitness(UInt64 aExpD, UInt64 n, UInt64 s)
        {
            UInt64 aExp = aExpD;
            if ((aExp == 1L) || (aExp == (n - 1L)))
            {
                return true;
            }
            for (UInt64 i = 1L; i < s; i += (UInt64)1L)
            {
                aExp = (aExp * aExp) % n;
                if (aExp == (n - 1L))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if value n is a prime number
        /// </summary>
        /// <param name="n">Number to check</param>
        /// <returns>true if n is prime</returns>
        public static Boolean CheckPrimality(UInt64 n)
        {
            if ((n % 2L) == 0L)
				return false;
            if ((n % 3L) == 0L)
				return false;
            if ((n % 5L) == 0L)
				return false;
            if ((n % 7L) == 0L)
				return false;

            UInt64 s = 0L;
            var d = n - 1L;

            do
            {
                s += 1L;
                d /= 2L;
            }
            while ((d % 2L) == 0L);

            UInt64 a = 2L;
            if (!CheckWitness(IntPow(a, d, n), n, s))
				return false;
            a = 7L;
            if (!CheckWitness(IntPow(a, d, n), n, s))
				return false;
            a = 0x3dL;
            return CheckWitness(IntPow(a, d, n), n, s);
        }
    }
}