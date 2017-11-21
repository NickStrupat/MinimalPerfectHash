using System;
using System.Text;
using Xunit;

namespace Test
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {

	        // Create a unique string generator
	        var keyGenerator = new KeyGenerator(2_000_000);

	        // Derivate a minimum perfect hash function
	        Console.WriteLine("Generating minimum perfect hash function for {0} keys", keyGenerator.KeyCount);
	        var start = DateTime.Now;
	        var hashFunction = MPHTest.MPH.MinPerfectHash.Create(keyGenerator, 1);

	        Console.WriteLine("Completed in {0:0.000000} s", DateTime.Now.Subtract(start).TotalMilliseconds / 1000.0);

	        // Show the extra hash space necessary
	        Console.WriteLine("Hash function map {0} keys to {1} hashes (load factor: {2:0.000000}%)",
		        keyGenerator.KeyCount, hashFunction.N,
		        ((keyGenerator.KeyCount * 100) / (Double)hashFunction.N));

	        // Check for any collision
	        var used = new System.Collections.BitArray((Int32)hashFunction.N);
	        keyGenerator.Rewind();
	        start = DateTime.Now;
	        for (var test = 0U; test < keyGenerator.KeyCount; test++)
	        {
		        var hash = (Int32)hashFunction.Search(keyGenerator.Read());
		        if (used[hash])
		        {
					Assert.True(false, $"FAILED - Collision detected at {test}");
			        Console.WriteLine("FAILED - Collision detected at {0}", test);
			        return;
		        }
				used[hash] = true;
	        }
	        var end = DateTime.Now.Subtract(start).TotalMilliseconds;
	        Console.WriteLine("PASS - No collision detected");

	        Console.WriteLine("Total scan time : {0:0.000000} s", end / 1000.0);
	        Console.WriteLine("Average key hash time : {0} ms", end / (Double)keyGenerator.KeyCount);
		}

		class KeyGenerator : MPHTest.MPH.IKeySource
		{
			UInt32 currentKey;

			public UInt32 KeyCount { get; }

			public KeyGenerator(UInt32 keyCount) => KeyCount = keyCount;

			public Byte[] Read() => Encoding.UTF8.GetBytes($"KEY-{currentKey++}");

			public void Rewind() => currentKey = 0;

			UInt32 MPHTest.MPH.IKeySource.NbKeys => KeyCount;
		}
	}
}
