using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MinimalPerfectHash
{
	public sealed class MphReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
	{
		private readonly (Byte flag, KeyValuePair<TKey, TValue> kvp)[] table;
		private readonly Func<TKey, Byte[]> getKeyBytes;
		private readonly IEqualityComparer<TKey> comparer;
		private readonly MphFunction hashFunction;

		public Int32 Count { get; }
		private const Double DefaultLoadFactor = 0.99d;

		/// <param name="loadFactor">0.5 &lt; loadFactor &gt; 0.99</param>
		public MphReadOnlyDictionary(
			IEnumerable<KeyValuePair<TKey, TValue>> dictionary,
			IEqualityComparer<TKey> comparer,
			Func<TKey, Byte[]> getKeyBytes,
			Double loadFactor = DefaultLoadFactor)
		{
			this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
			this.getKeyBytes = getKeyBytes ?? throw new ArgumentNullException(nameof(getKeyBytes));
			if (loadFactor < 0.5)
				loadFactor = 0.5;
			if (loadFactor >= 0.99)
				loadFactor = 0.99;
			var keyGenerator = new KeyGenerator(dictionary ?? throw new ArgumentNullException(nameof(dictionary)), getKeyBytes);
			Count = checked ((Int32) keyGenerator.KeyCount);
			hashFunction = new MphFunction(keyGenerator, loadFactor);
			table = new (Byte, KeyValuePair<TKey, TValue>)[hashFunction.N];
			foreach (var kvp in keyGenerator.Dictionary)
			{
				var bytes = getKeyBytes(kvp.Key);
				var hash = hashFunction.Search(bytes);
				table[hash] = (1, kvp);
			}
		}

		/// <param name="loadFactor">0.5 &lt; loadFactor &gt; 0.99</param>
		public MphReadOnlyDictionary(
			IEnumerable<KeyValuePair<TKey, TValue>> dictionary,
			Func<TKey, Byte[]> getKeyBytes,
			Double loadFactor = DefaultLoadFactor)
		: this(dictionary, EqualityComparer<TKey>.Default, getKeyBytes, loadFactor)
		{}

		/// <param name="loadFactor">0.5 &lt; loadFactor &gt; 0.99</param>
		public MphReadOnlyDictionary(
			Dictionary<TKey, TValue> dictionary,
			Func<TKey, Byte[]> getKeyBytes,
			Double loadFactor = DefaultLoadFactor)
		: this (dictionary, dictionary.Comparer, getKeyBytes, loadFactor)
		{}

		private IEnumerable<KeyValuePair<TKey, TValue>> GetEnumerable()
		{
			for (var i = 0; i < table.Length; i++)
				if (table[i].flag == 1)
					yield return table[i].kvp;
		}

		public IEnumerable<TKey> Keys => GetEnumerable().Select(x => x.Key);
		public IEnumerable<TValue> Values => GetEnumerable().Select(x => x.Value);

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => GetEnumerable().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public Boolean ContainsKey(TKey key) => TryGetValue(key, out var _);

		public Boolean TryGetValue(TKey key, out TValue value)
		{
			var bytes = getKeyBytes(key);
			var hash = hashFunction.Search(bytes);
			if (hash < table.Length)
			{
				var entry = table[hash];
				if (comparer.Equals(entry.kvp.Key, key))
				{
					value = entry.kvp.Value;
					return true;
				}
			}
			value = default(TValue);
			return false;
		}

		public TValue this[TKey key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();

		private class KeyGenerator : IKeySource
		{
			internal readonly IEnumerable<KeyValuePair<TKey, TValue>> Dictionary;
			private IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
			private readonly Func<TKey, Byte[]> getKeyBytes;

			public KeyGenerator(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, Func<TKey, Byte[]> getKeyBytes)
			{
				switch (dictionary) {
					case ICollection<KeyValuePair<TKey, TValue>> icollection:
						KeyCount = (UInt32) icollection.Count;
						this.Dictionary = dictionary;
						break;
					case IReadOnlyCollection<KeyValuePair<TKey, TValue>> ireadOnlyCollection:
						KeyCount = (UInt32) ireadOnlyCollection.Count;
						this.Dictionary = dictionary;
						break;
					default:
						var list = dictionary.ToList();
						this.Dictionary = list;
						KeyCount = (UInt32) list.Count;
						break;
				}
				this.enumerator = this.Dictionary.GetEnumerator();
				this.getKeyBytes = getKeyBytes;
			}

			public UInt32 KeyCount { get; }

			public Byte[] Read()
			{
				if (!enumerator.MoveNext())
					throw new InvalidOperationException();
				return getKeyBytes(enumerator.Current.Key);
			}

			public void Rewind()
			{
				enumerator = Dictionary.GetEnumerator(); // some enumerators might not have `Reset()` implemented
			}
		}
	}
}
