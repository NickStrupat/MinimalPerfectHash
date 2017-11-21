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
    internal struct BucketSortedList
    {
        public uint BucketsList;
        public uint Size;
    }

    internal class Buckets
    {
        private const uint KeysPerBucket = 4; // average number of keys per bucket
        private const uint MaxProbesBase = 1 << 20;

        struct Item
        {
            public uint F;
            public uint H;
        }

        struct Bucket
        {
            public uint ItemsList; // offset
            uint _sizeBucketID;
			
            public uint Size	   { get { return _sizeBucketID; } set { _sizeBucketID = value; } }
            public uint BucketID   { get { return _sizeBucketID; } set { _sizeBucketID = value; } }
        }

        struct MapItem
        {
            public uint F;
            public uint H;
            public uint BucketNum;
        };


        Bucket[]   _buckets;
        Item[]     _items;
        readonly uint _nbuckets;	    // number of buckets
        readonly uint _n;			    // number of bins
        readonly uint _m;				// number of keys
        readonly IKeySource _keySource;

        public uint NBuckets
        {
            get { return _nbuckets; }
        }
        public uint N
        {
            get { return _n; }
        }

        public Buckets(IKeySource keySource, double c)
        {
            _keySource = keySource;

            var loadFactor = c;
            _m = keySource.NbKeys;
            _nbuckets = _m/KeysPerBucket + 1;
	
            if(loadFactor < 0.5 )  loadFactor = 0.5;
            if(loadFactor >= 0.99) loadFactor = 0.99;
        
            _n = (uint)(_m/(loadFactor)) + 1;
	
            if(_n % 2 == 0) _n++;
            for(;;)
            {
                if(MillerRabin.CheckPrimality(_n)) break;
                _n += 2; // just odd numbers can be primes for n > 2
            }
            
            _buckets = new Bucket[_nbuckets]; 
            _items   = new Item[_m];

        }

        bool BucketsInsert(MapItem[] mapItems, uint itemIdx)
        {
            var bucketIdx = mapItems[itemIdx].BucketNum;
            var p = _buckets[bucketIdx].ItemsList;

            for (uint i = 0; i < _buckets[bucketIdx].Size; i++)
            {
                if (_items[p].F == mapItems[itemIdx].F && _items[p].H == mapItems[itemIdx].H)
                {
                    return false;
                }
                p++;
            }
            _items[p].F = mapItems[itemIdx].F;
            _items[p].H = mapItems[itemIdx].H;
            _buckets[bucketIdx].Size++;
            return true;
        }

        void BucketsClean()
        {
            for (uint i = 0; i < _nbuckets; i++)
                _buckets[i].Size = 0;
        }

        public bool MappingPhase(out uint hashSeed, out uint maxBucketSize)
        {
            var hl = new uint[3];
            var mapItems = new MapItem[_m];
            uint mappingIterations = 1000;
            var rdm = new Random(111);
	
            maxBucketSize = 0;
            for (; ; )
            {
                mappingIterations--;
                hashSeed = (uint)rdm.Next((int)_m); // ((cmph_uint32)rand() % this->_m);

                BucketsClean();

                _keySource.Rewind();

                uint i;
                for (i = 0; i < _m; i++)
                {
                    JenkinsHash.HashVector(hashSeed, _keySource.Read(), hl);

                    uint g = hl[0] % _nbuckets;
                    mapItems[i].F = hl[1] % _n;
                    mapItems[i].H = hl[2] % (_n - 1) + 1;
                    mapItems[i].BucketNum = g;

                    _buckets[g].Size++;
                    if (_buckets[g].Size > maxBucketSize)
                    {
                        maxBucketSize = _buckets[g].Size;
                    }
                }
                _buckets[0].ItemsList = 0;
                for (i = 1; i < _nbuckets; i++)
                {
                    _buckets[i].ItemsList = _buckets[i - 1].ItemsList + _buckets[i - 1].Size;
                    _buckets[i - 1].Size = 0;
                }
                _buckets[i - 1].Size = 0;
                for (i = 0; i < _m; i++)
                {
                    if (!BucketsInsert(mapItems, i))
                        break;
                }
                if (i == _m)
                {
                    return true; // SUCCESS
                }

                if (mappingIterations == 0)
                {
                    return false;
                }
            }
        }

        public BucketSortedList[] OrderingPhase(uint maxBucketSize)
        {
            var sortedLists = new BucketSortedList[maxBucketSize + 1];
            var inputBuckets = _buckets;
            var inputItems = _items;
            uint i;
            uint bucketSize, position;

            for (i = 0; i < _nbuckets; i++)
            {
                bucketSize = inputBuckets[i].Size;
                if (bucketSize == 0)
                    continue;
                sortedLists[bucketSize].Size++;
            }
            
            sortedLists[1].BucketsList = 0;
            // Determine final position of list of buckets into the contiguous array that will store all the buckets
            for (i = 2; i <= maxBucketSize; i++)
            {
                sortedLists[i].BucketsList = sortedLists[i - 1].BucketsList + sortedLists[i - 1].Size;
                sortedLists[i - 1].Size = 0;
            }
            
            sortedLists[i - 1].Size = 0;
            // Store the buckets in a new array which is sorted by bucket sizes
            var outputBuckets = new Bucket[_nbuckets];

            for (i = 0; i < _nbuckets; i++)
            {
                bucketSize = inputBuckets[i].Size;
                if (bucketSize == 0)
                {
                    continue;
                }
                
                position = sortedLists[bucketSize].BucketsList + sortedLists[bucketSize].Size;
                outputBuckets[position].BucketID = i;
                outputBuckets[position].ItemsList = inputBuckets[i].ItemsList;
                sortedLists[bucketSize].Size++;
            }
            
            _buckets = outputBuckets;

            // Store the items according to the new order of buckets.
            var outputItems = new Item[_n];
            position = 0;
            
            for (bucketSize = 1; bucketSize <= maxBucketSize; bucketSize++)
            {
                for (i = sortedLists[bucketSize].BucketsList;
                     i < sortedLists[bucketSize].Size + sortedLists[bucketSize].BucketsList;
                     i++)
                {
                    var position2 = outputBuckets[i].ItemsList;
                    outputBuckets[i].ItemsList = position;
                    for (uint j = 0; j < bucketSize; j++)
                    {
                        outputItems[position].F = inputItems[position2].F;
                        outputItems[position].H = inputItems[position2].H;
                        position++;
                        position2++;
                    }
                }
            }

            //Return the items sorted in new order and free the old items sorted in old order
            _items = outputItems;
            return sortedLists;
        }

        bool PlaceBucketProbe(uint probe0Num, uint probe1Num, uint bucketNum, uint size, BitArray occupTable)
        {
            uint i;
            uint position;

            var p = _buckets[bucketNum].ItemsList;

            // try place bucket with probe_num
            for(i = 0; i < size; i++) // placement
            {
                position = (uint)((_items[p].F + ((ulong)_items[p].H) * probe0Num + probe1Num) % _n);
                if (occupTable.GetBit(position))
                {
                    break;
                }
                occupTable.SetBit(position);
                p++;
            }
            if(i != size) // Undo the placement
            {
                p= _buckets[bucketNum].ItemsList;
                for (; ; )
                {
                    if (i == 0)
                    {
                        break;
                    }
                    position = (uint)((_items[p].F + ((ulong)_items[p].H) * probe0Num + probe1Num) % _n);
                    occupTable.UnSetBit(position);

                    // 				([position/32]^=(1<<(position%32));
                    p++;
                    i--;
                }
                return false;
            } 	
            return true;
        }

        public bool SearchingPhase(uint maxBucketSize, BucketSortedList[] sortedLists, uint[] dispTable)
        {
            var maxProbes = (uint)(((Math.Log(_m) / Math.Log(2.0)) / 20) * MaxProbesBase);
            uint i;
            var occupTable = new BitArray((int) (((_n + 31) / 32) * sizeof(uint)));

            for(i = maxBucketSize; i > 0; i--)
            {
                uint probeNum = 0;
                uint probe0Num = 0;
                uint probe1Num = 0;
                var sortedListSize = sortedLists[i].Size;
                while(sortedLists[i].Size != 0)
                {
                    var currBucket = sortedLists[i].BucketsList;
                    uint nonPlacedBucket=0;
                    for (uint j = 0; j < sortedLists[i].Size; j++)
                    {
                        // if bucket is successfully placed remove it from list
                        if (PlaceBucketProbe(probe0Num, probe1Num, currBucket, i, occupTable))
                        {	
                            dispTable[_buckets[currBucket].BucketID] = probe0Num + probe1Num * _n;
                        } 
                        else
                        {
                            _buckets[nonPlacedBucket + sortedLists[i].BucketsList].ItemsList = _buckets[currBucket].ItemsList;
                            _buckets[nonPlacedBucket + sortedLists[i].BucketsList].BucketID = _buckets[currBucket].BucketID;
                            nonPlacedBucket++;
                        }
                        currBucket++;
                    }
                    sortedLists[i].Size = nonPlacedBucket;
                    probe0Num++;
                    if(probe0Num >= _n)
                    {
                        probe0Num -= _n;
                        probe1Num++;
                    }
                    probeNum++;
                    if (probeNum < maxProbes && probe1Num < _n) continue;
                    sortedLists[i].Size = sortedListSize;
                    return false;
                }
                sortedLists[i].Size = sortedListSize;
            }
            return true;            
        }
    }
}