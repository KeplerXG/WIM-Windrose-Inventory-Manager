using System;
using System.Collections;
using System.Collections.Generic;

namespace WIM;

public sealed class BsonDocument : IEnumerable<KeyValuePair<string, BsonValue>>, IEnumerable
{
	private readonly List<string> _order = new List<string>();

	private readonly Dictionary<string, BsonValue> _map = new Dictionary<string, BsonValue>(StringComparer.Ordinal);

	public int Count => _order.Count;

	public BsonValue this[string key]
	{
		get
		{
			return _map[key];
		}
		set
		{
			if (!_map.ContainsKey(key))
			{
				_order.Add(key);
			}
			_map[key] = value;
		}
	}

	public bool ContainsKey(string key)
	{
		return _map.ContainsKey(key);
	}

	public bool TryGetValue(string key, out BsonValue? value)
	{
		BsonValue value2;
		bool result = _map.TryGetValue(key, out value2);
		value = value2;
		return result;
	}

	public void Remove(string key)
	{
		if (_map.Remove(key))
		{
			_order.Remove(key);
		}
	}

	public IEnumerator<KeyValuePair<string, BsonValue>> GetEnumerator()
	{
		foreach (string item in _order)
		{
			yield return new KeyValuePair<string, BsonValue>(item, _map[item]);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public BsonValue? Navigate(string path)
	{
		BsonValue value = BsonValue.FromDocument(this);
		string[] array = path.Split('.');
		foreach (string key in array)
		{
			if (value == null || !value.IsDocument)
			{
				return null;
			}
			value.AsDocument().TryGetValue(key, out value);
		}
		return value;
	}
}
