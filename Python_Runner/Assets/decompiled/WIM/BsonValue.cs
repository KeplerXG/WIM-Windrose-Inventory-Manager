namespace WIM;

public sealed class BsonValue
{
	public readonly BsonType Type;

	private readonly object? _v;

	public static readonly BsonValue Null = new BsonValue(BsonType.Null, null);

	public bool IsNull => Type == BsonType.Null;

	public bool IsDocument
	{
		get
		{
			if (Type != BsonType.Document)
			{
				return Type == BsonType.Array;
			}
			return true;
		}
	}

	private BsonValue(BsonType t, object? v)
	{
		Type = t;
		_v = v;
	}

	public static BsonValue FromDouble(double v)
	{
		return new BsonValue(BsonType.Double, v);
	}

	public static BsonValue FromString(string v)
	{
		return new BsonValue(BsonType.String, v);
	}

	public static BsonValue FromDocument(BsonDocument v)
	{
		return new BsonValue(BsonType.Document, v);
	}

	public static BsonValue FromArray(BsonDocument v)
	{
		return new BsonValue(BsonType.Array, v);
	}

	public static BsonValue FromBinary(byte[] d, byte sub)
	{
		return new BsonValue(BsonType.Binary, new BsonBinary(d, sub));
	}

	public static BsonValue FromBool(bool v)
	{
		return new BsonValue(BsonType.Bool, v);
	}

	public static BsonValue FromDateTime(long v)
	{
		return new BsonValue(BsonType.DateTime, v);
	}

	public static BsonValue FromInt32(int v)
	{
		return new BsonValue(BsonType.Int32, v);
	}

	public static BsonValue FromInt64(long v)
	{
		return new BsonValue(BsonType.Int64, v);
	}

	public double AsDouble()
	{
		return (double)_v;
	}

	public string AsString()
	{
		return (string)_v;
	}

	public BsonDocument AsDocument()
	{
		return (BsonDocument)_v;
	}

	public BsonBinary AsBinary()
	{
		return (BsonBinary)_v;
	}

	public bool AsBool()
	{
		return (bool)_v;
	}

	public long AsDateTime()
	{
		return (long)_v;
	}

	public int AsInt32()
	{
		return (int)_v;
	}

	public long AsInt64()
	{
		return (long)_v;
	}

	public long TryAsLong()
	{
		return Type switch
		{
			BsonType.Int32 => AsInt32(), 
			BsonType.Int64 => AsInt64(), 
			BsonType.Double => (long)AsDouble(), 
			_ => 0L, 
		};
	}

	public override string ToString()
	{
		return Type switch
		{
			BsonType.Null => "null", 
			BsonType.Bool => AsBool().ToString(), 
			BsonType.Int32 => AsInt32().ToString(), 
			BsonType.Int64 => AsInt64().ToString(), 
			BsonType.Double => AsDouble().ToString("G"), 
			BsonType.String => AsString(), 
			BsonType.Document => $"[doc {AsDocument().Count} fields]", 
			BsonType.Array => $"[array {AsDocument().Count} items]", 
			_ => $"<{Type}>", 
		};
	}
}
