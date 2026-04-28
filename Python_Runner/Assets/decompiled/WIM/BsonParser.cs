using System;
using System.IO;
using System.Text;

namespace WIM;

public static class BsonParser
{
	public static BsonDocument Parse(byte[] data, int pos = 0)
	{
		int num = BitConverter.ToInt32(data, pos);
		int num2 = pos + num;
		pos += 4;
		BsonDocument bsonDocument = new BsonDocument();
		while (pos < num2 - 1)
		{
			byte b = data[pos++];
			if (b == 0)
			{
				break;
			}
			string text = ReadCString(data, ref pos);
			switch (b)
			{
			case 1:
				bsonDocument[text] = BsonValue.FromDouble(BitConverter.ToDouble(data, pos));
				pos += 8;
				break;
			case 2:
			{
				int num6 = BitConverter.ToInt32(data, pos);
				pos += 4;
				bsonDocument[text] = BsonValue.FromString(Encoding.UTF8.GetString(data, pos, num6 - 1));
				pos += num6;
				break;
			}
			case 3:
			{
				int num5 = BitConverter.ToInt32(data, pos);
				bsonDocument[text] = BsonValue.FromDocument(Parse(data, pos));
				pos += num5;
				break;
			}
			case 4:
			{
				int num4 = BitConverter.ToInt32(data, pos);
				bsonDocument[text] = BsonValue.FromArray(Parse(data, pos));
				pos += num4;
				break;
			}
			case 5:
			{
				int num3 = BitConverter.ToInt32(data, pos);
				pos += 4;
				byte sub = data[pos++];
				byte[] array = new byte[num3];
				Buffer.BlockCopy(data, pos, array, 0, num3);
				bsonDocument[text] = BsonValue.FromBinary(array, sub);
				pos += num3;
				break;
			}
			case 8:
				bsonDocument[text] = BsonValue.FromBool(data[pos++] != 0);
				break;
			case 9:
				bsonDocument[text] = BsonValue.FromDateTime(BitConverter.ToInt64(data, pos));
				pos += 8;
				break;
			case 10:
				bsonDocument[text] = BsonValue.Null;
				break;
			case 16:
				bsonDocument[text] = BsonValue.FromInt32(BitConverter.ToInt32(data, pos));
				pos += 4;
				break;
			case 18:
				bsonDocument[text] = BsonValue.FromInt64(BitConverter.ToInt64(data, pos));
				pos += 8;
				break;
			default:
				throw new InvalidDataException($"Unknown BSON type 0x{b:X2} at pos {pos - 1} field '{text}'");
			}
		}
		return bsonDocument;
	}

	private static string ReadCString(byte[] data, ref int pos)
	{
		int num = pos;
		while (pos < data.Length && data[pos] != 0)
		{
			pos++;
		}
		string result = Encoding.UTF8.GetString(data, num, pos - num);
		pos++;
		return result;
	}
}
