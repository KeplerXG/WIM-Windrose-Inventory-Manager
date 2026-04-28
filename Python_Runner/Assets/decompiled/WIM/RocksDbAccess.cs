using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WIM;

public static class RocksDbAccess
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate nint D_IterCreate(nint db, nint rOpts, nint cfHandle);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void D_IterVoid(nint it);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate byte D_IterBool(nint it);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate nint D_IterData(nint it, ref nuint len);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate nint D_VoidReturn();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate nint D_Open(nint opts, string path, int numCf, nint[] cfNames, nint[] cfOpts, nint[] cfHandles, byte errorIfLogFileExists, ref nint err);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate nint D_Get(nint db, nint rOpts, nint cfHandle, byte[] key, nuint keyLen, out nuint valLen, ref nint err);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void D_FreePtr(nint ptr);

	private const int BlockSize = 32768;

	private static readonly uint[] _crcTable = BuildCrcTable();

	private static readonly string[] CfNames = new string[5] { "default", "R5BLPlayer", "R5BLShip", "R5BLBuilding", "R5BLActor_BuildingBlock" };

	private static uint[] BuildCrcTable()
	{
		uint[] array = new uint[256];
		for (int i = 0; i < 256; i++)
		{
			uint num = (uint)i;
			for (int j = 0; j < 8; j++)
			{
				num = (((num & 1) != 0) ? ((num >> 1) ^ 0x82F63B78u) : (num >> 1));
			}
			array[i] = num;
		}
		return array;
	}

	private static uint Crc32C(byte[] data, int offset, int count)
	{
		uint num = uint.MaxValue;
		for (int i = offset; i < offset + count; i++)
		{
			num = (num >> 8) ^ _crcTable[(num ^ data[i]) & 0xFF];
		}
		return num ^ 0xFFFFFFFFu;
	}

	private static uint MaskedCrc(byte[] data, int offset, int count)
	{
		uint num = Crc32C(data, offset, count);
		return ((num >> 15) | (num << 17)) + 2726488792u;
	}

	public static (long value, int nextPos) ReadVarint(byte[] data, int pos)
	{
		long num = 0L;
		int num2 = 0;
		while (pos < data.Length)
		{
			byte b = data[pos++];
			num |= (long)(b & 0x7F) << num2;
			if ((b & 0x80) == 0)
			{
				break;
			}
			num2 += 7;
		}
		return (value: num, nextPos: pos);
	}

	public static byte[] WriteVarint(long n)
	{
		List<byte> list = new List<byte>(10);
		do
		{
			byte b = (byte)(n & 0x7F);
			n >>= 7;
			if (n != 0L)
			{
				b |= 0x80;
			}
			list.Add(b);
		}
		while (n != 0L);
		return list.ToArray();
	}

	public static PlayerSaveData? ReadFromWal(string saveDir)
	{
		string[] array = Directory.GetFiles(saveDir, "*.log").Concat(Directory.GetFiles(saveDir, "*.wal")).OrderBy<string, string>((string f) => f, StringComparer.OrdinalIgnoreCase).ToArray();
		if (array.Length == 0)
		{
			return null;
		}
		for (int num = array.Length - 1; num >= 0; num--)
		{
			PlayerSaveData playerSaveData = TryReadWalFile(array[num], saveDir);
			if (playerSaveData != null)
			{
				return playerSaveData;
			}
		}
		return null;
	}

	private static PlayerSaveData? TryReadWalFile(string walPath, string saveDir)
	{
		byte[] array;
		try
		{
			array = File.ReadAllBytes(walPath);
		}
		catch
		{
			return null;
		}
		using MemoryStream memoryStream = new MemoryStream();
		int i;
		for (i = 0; i + 7 <= array.Length; i += 32768)
		{
			ushort val = BitConverter.ToUInt16(array, i + 4);
			byte b = array[i + 6];
			int num = i + 7;
			int count = Math.Min(val, array.Length - num);
			if (b >= 1 && b <= 4)
			{
				memoryStream.Write(array, num, count);
			}
		}
		byte[] array2 = memoryStream.ToArray();
		if (array2.Length < 12)
		{
			return null;
		}
		long num2 = 0L;
		long num3 = 0L;
		byte[] array3 = null;
		byte[] array4 = null;
		i = 0;
		while (i + 12 <= array2.Length)
		{
			try
			{
				long num4 = BitConverter.ToInt64(array2, i);
				int num5 = BitConverter.ToInt32(array2, i + 8);
				int num6 = i + 12;
				long num7 = num4 + Math.Max(0, num5 - 1);
				if (num7 > num2)
				{
					num2 = num7;
				}
				for (int j = 0; j < num5 && num6 < array2.Length; j++)
				{
					byte b2 = array2[num6++];
					switch (b2)
					{
					case 1:
					case 5:
					{
						long num8 = 0L;
						if (b2 == 5)
						{
							(num8, num6) = ReadVarint(array2, num6);
						}
						(long value, int nextPos) tuple3 = ReadVarint(array2, num6);
						long item2 = tuple3.value;
						num6 = tuple3.nextPos;
						byte[] array5 = new byte[(int)item2];
						Buffer.BlockCopy(array2, num6, array5, 0, (int)item2);
						num6 += (int)item2;
						(long value, int nextPos) tuple4 = ReadVarint(array2, num6);
						long item3 = tuple4.value;
						num6 = tuple4.nextPos;
						byte[] array6 = new byte[(int)item3];
						Buffer.BlockCopy(array2, num6, array6, 0, (int)item3);
						num6 += (int)item3;
						if (num8 == 2 && item2 == 32 && item3 > 1000 && array6.Length >= 4 && BitConverter.ToInt32(array6, 0) == item3)
						{
							array3 = array5;
							array4 = array6;
							num3 = num4;
						}
						continue;
					}
					case 0:
					case 4:
					{
						if (b2 == 4)
						{
							ReadVarint(array2, num6);
						}
						(long value, int nextPos) tuple = ReadVarint(array2, num6);
						long item = tuple.value;
						num6 = tuple.nextPos;
						num6 += (int)item;
						continue;
					}
					}
					break;
				}
				i = num6;
			}
			catch
			{
				break;
			}
		}
		if (array3 == null || array4 == null)
		{
			return null;
		}
		return new PlayerSaveData
		{
			Sequence = ((num2 > 0) ? num2 : num3),
			CfId = 2,
			PlayerKey = array3,
			BsonBytes = array4,
			SaveDir = saveDir
		};
	}

	public static (long LastSeq, long NextFileNum, long LogNum) ParseManifest(string saveDir)
	{
		string[] array = Directory.GetFiles(saveDir, "MANIFEST-*").OrderBy<string, string>((string f) => f, StringComparer.OrdinalIgnoreCase).ToArray();
		if (array.Length == 0)
		{
			return (LastSeq: 0L, NextFileNum: 0L, LogNum: 0L);
		}
		byte[] array2;
		try
		{
			array2 = File.ReadAllBytes(array[^1]);
		}
		catch
		{
			return (LastSeq: 0L, NextFileNum: 0L, LogNum: 0L);
		}
		long num = 0L;
		long num2 = 0L;
		long num3 = 0L;
		int num4 = 0;
		while (num4 < array2.Length && num4 + 7 <= array2.Length)
		{
			int num5 = BitConverter.ToUInt16(array2, num4 + 4);
			int num6 = num4 + 7;
			int num7 = Math.Min(num5, array2.Length - num6);
			byte[] array3 = new byte[num7];
			Buffer.BlockCopy(array2, num6, array3, 0, num7);
			num4 += 7 + num5;
			int num8 = num4 % 32768;
			if (num8 > 0 && num8 < 7)
			{
				num4 += 32768 - num8;
			}
			int num9 = 0;
			while (num9 < array3.Length)
			{
				try
				{
					long num10;
					(num10, num9) = ReadVarint(array3, num9);
					switch (num10)
					{
					case 2L:
					{
						(long value, int nextPos) tuple4 = ReadVarint(array3, num9);
						long item3 = tuple4.value;
						num9 = tuple4.nextPos;
						num3 = Math.Max(num3, item3);
						break;
					}
					case 3L:
					{
						(long value, int nextPos) tuple3 = ReadVarint(array3, num9);
						long item2 = tuple3.value;
						num9 = tuple3.nextPos;
						num2 = Math.Max(num2, item2);
						break;
					}
					case 4L:
					{
						(long value, int nextPos) tuple2 = ReadVarint(array3, num9);
						long item = tuple2.value;
						num9 = tuple2.nextPos;
						num = Math.Max(num, item);
						break;
					}
					default:
						num9++;
						break;
					}
				}
				catch
				{
					num9++;
				}
			}
		}
		return (LastSeq: num, NextFileNum: num2, LogNum: num3);
	}

	public static bool WriteWal(string saveDir, long seq, long fileNum, int cfId, byte[] playerKey, byte[] bsonBytes)
	{
		string path;
		if (fileNum > 0)
		{
			path = Path.Combine(saveDir, $"{fileNum:D6}.log");
		}
		else
		{
			long num = 0L;
			string[] files = Directory.GetFiles(saveDir);
			for (int i = 0; i < files.Length; i++)
			{
				if (long.TryParse(Path.GetFileNameWithoutExtension(files[i]), out var result))
				{
					num = Math.Max(num, result);
				}
			}
			path = Path.Combine(saveDir, $"{num + 1:D6}.log");
		}
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.Write(BitConverter.GetBytes(seq), 0, 8);
		memoryStream.Write(BitConverter.GetBytes(1), 0, 4);
		memoryStream.WriteByte(5);
		byte[] array = WriteVarint(cfId);
		byte[] array2 = WriteVarint(playerKey.Length);
		byte[] array3 = WriteVarint(bsonBytes.Length);
		memoryStream.Write(array, 0, array.Length);
		memoryStream.Write(array2, 0, array2.Length);
		memoryStream.Write(playerKey, 0, playerKey.Length);
		memoryStream.Write(array3, 0, array3.Length);
		memoryStream.Write(bsonBytes, 0, bsonBytes.Length);
		byte[] array4 = memoryStream.ToArray();
		using MemoryStream memoryStream2 = new MemoryStream();
		int num2 = 0;
		int num3 = array4.Length;
		while (num2 < num3)
		{
			int num4 = Math.Min(32761, num3 - num2);
			bool flag = num2 == 0;
			bool flag2 = num2 + num4 >= num3;
			byte b = (byte)((flag && flag2) ? 1 : (flag ? 2 : (flag2 ? 4 : 3)));
			byte[] array5 = new byte[1 + num4];
			array5[0] = b;
			Buffer.BlockCopy(array4, num2, array5, 1, num4);
			uint value = MaskedCrc(array5, 0, array5.Length);
			memoryStream2.Write(BitConverter.GetBytes(value), 0, 4);
			memoryStream2.Write(BitConverter.GetBytes((ushort)num4), 0, 2);
			memoryStream2.WriteByte(b);
			memoryStream2.Write(array4, num2, num4);
			num2 += num4;
			long num5 = memoryStream2.Length % 32768;
			if (num5 > 0 && num2 >= num3)
			{
				byte[] array6 = new byte[32768 - num5];
				memoryStream2.Write(array6, 0, array6.Length);
			}
		}
		try
		{
			File.WriteAllBytes(path, memoryStream2.ToArray());
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static PlayerSaveData? ReadFromSstDirect(string saveDir)
	{
		string s = StripGuidSuffix(Path.GetFileName(saveDir)).ToUpperInvariant();
		byte[] bytes = Encoding.ASCII.GetBytes(s);
		foreach (string item in from f in Directory.GetFiles(saveDir, "*.sst")
			where new FileInfo(f).Length >= 4096
			orderby new FileInfo(f).Length descending, (!long.TryParse(Path.GetFileNameWithoutExtension(f), out var result)) ? 0 : result descending
			select f)
		{
			PlayerSaveData playerSaveData = TryScanSstDirect(item, bytes, saveDir);
			if (playerSaveData != null)
			{
				return playerSaveData;
			}
		}
		return null;
	}

	private static PlayerSaveData? TryScanSstDirect(string sstPath, byte[] guidBytes, string saveDir)
	{
		byte[] array;
		try
		{
			array = File.ReadAllBytes(sstPath);
		}
		catch
		{
			return null;
		}
		if (array.Length < 200)
		{
			return null;
		}
		for (int i = 0; i < array.Length - guidBytes.Length - 20; i++)
		{
			if (array[i] != 0)
			{
				continue;
			}
			int num = i + 1;
			if (num >= array.Length || array[num] != 40)
			{
				continue;
			}
			int num2 = i + 2;
			if (num2 >= array.Length)
			{
				continue;
			}
			var (num3, num4) = ReadVarint(array, num2);
			if (num3 < 100 || num3 > 20000000 || num4 + 40 + (int)num3 > array.Length)
			{
				continue;
			}
			bool flag = true;
			for (int j = 0; j < guidBytes.Length; j++)
			{
				if (array[num4 + j] != guidBytes[j])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				int srcOffset = num4 + 40;
				byte[] array2 = new byte[(int)num3];
				Buffer.BlockCopy(array, srcOffset, array2, 0, (int)num3);
				if (array2.Length >= 4 && BitConverter.ToInt32(array2, 0) == array2.Length)
				{
					return new PlayerSaveData
					{
						Sequence = 99999L,
						CfId = 2,
						PlayerKey = guidBytes,
						BsonBytes = array2,
						SaveDir = saveDir
					};
				}
			}
		}
		return null;
	}

	public static PlayerSaveData? ReadFromSst(string saveDir, string? dllPath = null)
	{
		string text = dllPath ?? FindRocksDbDll(saveDir);
		if (text == null)
		{
			return null;
		}
		nint num;
		try
		{
			num = NativeLibrary.Load(text);
		}
		catch
		{
			return null;
		}
		try
		{
			return ReadFromSstInternal(num, saveDir);
		}
		finally
		{
			NativeLibrary.Free(num);
		}
	}

	private static PlayerSaveData? ReadFromSstInternal(nint lib, string saveDir)
	{
		if (!NativeLibrary.TryGetExport(lib, "rocksdb_options_create", out var address) || !NativeLibrary.TryGetExport(lib, "rocksdb_readoptions_create", out var address2) || !NativeLibrary.TryGetExport(lib, "rocksdb_open_for_read_only_column_families", out var address3) || !NativeLibrary.TryGetExport(lib, "rocksdb_get_cf", out var address4) || !NativeLibrary.TryGetExport(lib, "rocksdb_free", out var address5) || !NativeLibrary.TryGetExport(lib, "rocksdb_close", out var address6))
		{
			return null;
		}
		D_VoidReturn delegateForFunctionPointer = Marshal.GetDelegateForFunctionPointer<D_VoidReturn>(address);
		D_VoidReturn delegateForFunctionPointer2 = Marshal.GetDelegateForFunctionPointer<D_VoidReturn>(address2);
		D_Open delegateForFunctionPointer3 = Marshal.GetDelegateForFunctionPointer<D_Open>(address3);
		D_Get delegateForFunctionPointer4 = Marshal.GetDelegateForFunctionPointer<D_Get>(address4);
		D_FreePtr delegateForFunctionPointer5 = Marshal.GetDelegateForFunctionPointer<D_FreePtr>(address5);
		D_FreePtr delegateForFunctionPointer6 = Marshal.GetDelegateForFunctionPointer<D_FreePtr>(address6);
		int num = CfNames.Length;
		nint opts = delegateForFunctionPointer();
		nint rOpts = delegateForFunctionPointer2();
		nint[] array = new nint[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = delegateForFunctionPointer();
		}
		byte[][] array2 = CfNames.Select((string s) => Encoding.ASCII.GetBytes(s + "\0")).ToArray();
		nint[] array3 = new nint[num];
		nint err = IntPtr.Zero;
		List<GCHandle> list = new List<GCHandle>();
		nint[] array4 = new nint[num];
		for (int num2 = 0; num2 < num; num2++)
		{
			GCHandle item = GCHandle.Alloc(array2[num2], GCHandleType.Pinned);
			list.Add(item);
			array4[num2] = item.AddrOfPinnedObject();
		}
		nint num3 = IntPtr.Zero;
		try
		{
			num3 = delegateForFunctionPointer3(opts, saveDir, num, array4, array, array3, 0, ref err);
			if (err != IntPtr.Zero || num3 == IntPtr.Zero)
			{
				return null;
			}
			string foundGuid = StripGuidSuffix(Path.GetFileName(saveDir)).ToUpperInvariant();
			byte[] array5 = TryGetByKey(lib, delegateForFunctionPointer4, delegateForFunctionPointer5, num3, rOpts, array3[1], Encoding.ASCII.GetBytes(foundGuid), ref err);
			if (array5 == null)
			{
				array5 = IterateFindPlayerBson(lib, num3, rOpts, array3[1], out foundGuid);
			}
			if (array5 == null)
			{
				return null;
			}
			return new PlayerSaveData
			{
				Sequence = 99999L,
				CfId = 2,
				PlayerKey = Encoding.ASCII.GetBytes(foundGuid),
				BsonBytes = array5,
				SaveDir = saveDir
			};
		}
		catch
		{
			return null;
		}
		finally
		{
			if (num3 != IntPtr.Zero)
			{
				delegateForFunctionPointer6(num3);
			}
			foreach (GCHandle item2 in list)
			{
				item2.Free();
			}
		}
	}

	private static string StripGuidSuffix(string folderName)
	{
		if (folderName.Length == 32 && folderName.All((char c) => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
		{
			return folderName;
		}
		if (folderName.Length > 32)
		{
			string text = folderName.Substring(0, 32);
			if (text.All((char c) => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
			{
				return text;
			}
		}
		return folderName;
	}

	private static byte[]? TryGetByKey(nint lib, D_Get fnGet, D_FreePtr fnFree, nint db, nint rOpts, nint cfHandle, byte[] key, ref nint errPtr)
	{
		errPtr = IntPtr.Zero;
		nuint valLen;
		nint num = fnGet(db, rOpts, cfHandle, key, (nuint)key.Length, out valLen, ref errPtr);
		if (errPtr != IntPtr.Zero || num == IntPtr.Zero || valLen == UIntPtr.Zero)
		{
			return null;
		}
		byte[] array = new byte[(uint)valLen];
		Marshal.Copy(num, array, 0, array.Length);
		fnFree(num);
		if (array.Length < 4 || BitConverter.ToInt32(array, 0) != array.Length)
		{
			return null;
		}
		return array;
	}

	private static byte[]? IterateFindPlayerBson(nint lib, nint db, nint rOpts, nint cfHandle, out string foundGuid)
	{
		foundGuid = "";
		try
		{
			if (!NativeLibrary.TryGetExport(lib, "rocksdb_create_iterator_cf", out var address) || !NativeLibrary.TryGetExport(lib, "rocksdb_iter_seek_to_first", out var address2) || !NativeLibrary.TryGetExport(lib, "rocksdb_iter_valid", out var address3) || !NativeLibrary.TryGetExport(lib, "rocksdb_iter_key", out var address4) || !NativeLibrary.TryGetExport(lib, "rocksdb_iter_value", out var address5) || !NativeLibrary.TryGetExport(lib, "rocksdb_iter_next", out var address6) || !NativeLibrary.TryGetExport(lib, "rocksdb_iter_destroy", out var address7))
			{
				return null;
			}
			D_IterCreate delegateForFunctionPointer = Marshal.GetDelegateForFunctionPointer<D_IterCreate>(address);
			D_IterVoid delegateForFunctionPointer2 = Marshal.GetDelegateForFunctionPointer<D_IterVoid>(address2);
			D_IterBool delegateForFunctionPointer3 = Marshal.GetDelegateForFunctionPointer<D_IterBool>(address3);
			D_IterData delegateForFunctionPointer4 = Marshal.GetDelegateForFunctionPointer<D_IterData>(address4);
			D_IterData delegateForFunctionPointer5 = Marshal.GetDelegateForFunctionPointer<D_IterData>(address5);
			D_IterVoid delegateForFunctionPointer6 = Marshal.GetDelegateForFunctionPointer<D_IterVoid>(address6);
			D_IterVoid delegateForFunctionPointer7 = Marshal.GetDelegateForFunctionPointer<D_IterVoid>(address7);
			nint it = delegateForFunctionPointer(db, rOpts, cfHandle);
			delegateForFunctionPointer2(it);
			try
			{
				while (delegateForFunctionPointer3(it) != 0)
				{
					nuint len = UIntPtr.Zero;
					nuint len2 = UIntPtr.Zero;
					nint source = delegateForFunctionPointer4(it, ref len);
					nint source2 = delegateForFunctionPointer5(it, ref len2);
					int num = (int)len;
					int num2 = (int)len2;
					if (num == 32 && num2 > 1000)
					{
						byte[] array = new byte[num];
						Marshal.Copy(source, array, 0, num);
						byte[] array2 = new byte[num2];
						Marshal.Copy(source2, array2, 0, num2);
						if (BitConverter.ToInt32(array2, 0) == num2)
						{
							foundGuid = Encoding.ASCII.GetString(array);
							return array2;
						}
					}
					delegateForFunctionPointer6(it);
				}
			}
			finally
			{
				delegateForFunctionPointer7(it);
			}
		}
		catch
		{
		}
		return null;
	}

	private static string? FindRocksDbDll(string saveDir)
	{
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		string[] array = new string[3]
		{
			Path.Combine(baseDirectory, "rocksdb.dll"),
			Path.Combine(baseDirectory, "..", "rocksdb.dll"),
			Path.Combine(saveDir, "rocksdb.dll")
		};
		foreach (string path in array)
		{
			if (File.Exists(path))
			{
				return Path.GetFullPath(path);
			}
		}
		try
		{
			DirectoryInfo? dir = new DirectoryInfo(Path.GetFullPath(saveDir));
			for (int i = 0; i < 20 && dir != null; i++)
			{
				string candidate = Path.Combine(dir.FullName, "rocksdb.dll");
				if (File.Exists(candidate))
				{
					return candidate;
				}
				dir = dir.Parent;
			}
		}
		catch
		{
		}
		return null;
	}
}
