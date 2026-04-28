namespace WIM;

public readonly struct BsonBinary(byte[] data, byte subtype)
{
	public readonly byte[] Data = data;

	public readonly byte Subtype = subtype;
}
