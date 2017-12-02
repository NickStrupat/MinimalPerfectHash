using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using MinimalPerfectHash;
using Xunit;

namespace Test
{
    public class UnitTest1
    {
        [Fact]
		public void Test1()
        {
	        const Double loadFactor = 1.0d;

			// Create a unique string generator
			const int KeyCount = 2_000_000;

	        // Derivate a minimum perfect hash function
	        Console.WriteLine("Generating minimum perfect hash function for {0} keys", KeyCount);
	        var start = DateTime.Now;
	        var hashFunction = new MphFunction<Int32>(Enumerable.Range(0, KeyCount), KeyCount, GetKeyBytes, loadFactor);

	        Console.WriteLine("Completed in {0:0.000000} s", DateTime.Now.Subtract(start).TotalMilliseconds / 1000.0);

	        // Show the extra hash space necessary
	        Console.WriteLine("Hash function map {0} keys to {1} hashes (load factor: {2:0.000000}%)",
		        KeyCount, hashFunction.MaxValue,
		        ((KeyCount * 100) / (Double)hashFunction.MaxValue));

	        // Check for any collision
	        var used = new System.Collections.BitArray((Int32)hashFunction.MaxValue);

	        start = DateTime.Now;
	        for (var test = 0U; test < KeyCount; test++)
	        {
		        var hash = (Int32)hashFunction.GetHash((Int32) test, GetKeyBytes);
		        if (used[hash])
		        {
					Assert.True(false, $"FAILED - Collision detected at {test}");
		        }
				used[hash] = true;
	        }
	        var end = DateTime.Now.Subtract(start).TotalMilliseconds;
	        Console.WriteLine("PASS - No collision detected");

	        Console.WriteLine("Total scan time : {0:0.000000} s", end / 1000.0);
	        Console.WriteLine("Average key hash time : {0} ms", end / (Double)KeyCount);

	        var dic = new Dictionary<Int32, String>((Int32)KeyCount);
	        for (var i = 0; i < KeyCount; i++)
	        {
		        dic.Add(i, i.ToString());
	        }
	        var mphrod = new MphReadOnlyDictionary<Int32, String>(dic, GetKeyBytes, loadFactor);
	        for (var i = 0; i < KeyCount; i++)
	        {
		        Assert.Equal(dic[i], mphrod[i]);
			}
			Assert.Equal(dic.OrderBy(x => x.Key), mphrod.OrderBy(x => x.Key));
			Assert.Equal(dic.Keys.OrderBy(x => x), mphrod.Keys.OrderBy(x => x));
			Assert.Equal(dic.Values.OrderBy(x => x), mphrod.Values.OrderBy(x => x));
        }

	    private Byte[] GetKeyBytes(Int32 i) => Encoding.UTF8.GetBytes($"KEY-{i}");

	    [Fact]
		public void OutOfRangeKeysThatCollideOnHashFunctionFailEqualityCheck()
		{
			const Int32 keyCount = 2_000_000;
			var dic = new Dictionary<Int32, String>(keyCount);
			for (var i = 0; i < keyCount; i++)
			{
				dic.Add(i, i.ToString());
			}
			var mphrod = new MphReadOnlyDictionary<Int32, String>(dic, GetKeyBytes);
			for (var i = 0; i < keyCount; i++)
			{
				Assert.False(mphrod.TryGetValue((i + keyCount), out var x));
			}
		}

	    [Fact]
	    public void HashFunctionSerialization()
		{
			const Int32 keyCount = 2_000;
			var hashFunction = new MphFunction<Int32>(Enumerable.Range(0, keyCount), keyCount, GetKeyBytes, 1);
			var table = new String[hashFunction.MaxValue];
			for (var i = 0; i < keyCount; i++)
			{
				var key = $"KEY-{i}";
				var hash = hashFunction.GetHash(Encoding.UTF8.GetBytes(key));
				table[hash] = key;
			}
			IFormatter formatter = new BinaryFormatter();
			using (var ms = new MemoryStream(1_000))
			{
				formatter.Serialize(ms, hashFunction);
				ms.Position = 0;
				var hashFunction2 = (MphFunction) formatter.Deserialize(ms);
				for (var i = 0; i < keyCount; i++)
				{
					var key = $"KEY-{i}";
					var hash = hashFunction2.GetHash(Encoding.UTF8.GetBytes(key));
					Assert.Equal(key, table[hash]);
				}
			}
		}
	}
}
