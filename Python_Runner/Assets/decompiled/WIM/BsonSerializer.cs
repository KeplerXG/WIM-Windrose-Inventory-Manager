using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WIM;

public static class BsonSerializer
{
	public static byte[] Serialize(BsonDocument doc)
	{
		using MemoryStream memoryStream = new MemoryStream();
		WriteDoc(memoryStream, doc);
		return memoryStream.ToArray();
	}

	private static void WriteDoc(MemoryStream ms, BsonDocument doc)
	{
		long position = ms.Position;
		ms.Write(BitConverter.GetBytes(0), 0, 4);
		foreach (var (key, val) in doc)
		{
			WriteField(ms, key, val);
		}
		ms.WriteByte(0);
		long position2 = ms.Position;
		int value = (int)(position2 - position);
		ms.Position = position;
		ms.Write(BitConverter.GetBytes(value), 0, 4);
		ms.Position = position2;
	}

	private static void WriteField(MemoryStream ms, string key, BsonValue val)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(key);
		ms.WriteByte((byte)val.Type);
		ms.Write(bytes, 0, bytes.Length);
		ms.WriteByte(0);
		switch (val.Type)
		{
		case BsonType.Double:
			ms.Write(BitConverter.GetBytes(val.AsDouble()), 0, 8);
			break;
		case BsonType.String:
		{
			byte[] bytes2 = Encoding.UTF8.GetBytes(val.AsString());
			int value = bytes2.Length + 1;
			ms.Write(BitConverter.GetBytes(value), 0, 4);
			ms.Write(bytes2, 0, bytes2.Length);
			ms.WriteByte(0);
			break;
		}
		case BsonType.Document:
		case BsonType.Array:
			WriteDoc(ms, val.AsDocument());
			break;
		case BsonType.Binary:
		{
			BsonBinary bsonBinary = val.AsBinary();
			ms.Write(BitConverter.GetBytes(bsonBinary.Data.Length), 0, 4);
			ms.WriteByte(bsonBinary.Subtype);
			ms.Write(bsonBinary.Data, 0, bsonBinary.Data.Length);
			break;
		}
		case BsonType.Bool:
			ms.WriteByte(val.AsBool() ? ((byte)1) : ((byte)0));
			break;
		case BsonType.DateTime:
			ms.Write(BitConverter.GetBytes(val.AsDateTime()), 0, 8);
			break;
		case BsonType.Int32:
			ms.Write(BitConverter.GetBytes(val.AsInt32()), 0, 4);
			break;
		case BsonType.Int64:
			ms.Write(BitConverter.GetBytes(val.AsInt64()), 0, 8);
			break;
		default:
			throw new InvalidDataException($"Cannot serialize BSON type {val.Type}");
		case BsonType.Null:
			break;
		}
	}
}
