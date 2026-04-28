using System;

namespace WIM;

public class PlayerSaveData
{
	public long Sequence { get; set; }

	public int CfId { get; set; } = 2;

	public byte[] PlayerKey { get; set; } = Array.Empty<byte>();

	public byte[] BsonBytes { get; set; } = Array.Empty<byte>();

	public string SaveDir { get; set; } = "";
}
