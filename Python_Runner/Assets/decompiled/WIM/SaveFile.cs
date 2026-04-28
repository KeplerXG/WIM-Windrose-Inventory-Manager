using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WIM;

public class SaveFile
{
	private PlayerSaveData _raw;

	public BsonDocument Doc { get; private set; }

	public string PlayerName { get; private set; } = "";

	public string PlayerGuid { get; private set; } = "";

	public bool IsModified { get; private set; }

	public string SaveDir => _raw.SaveDir;

	private SaveFile(PlayerSaveData raw, BsonDocument doc)
	{
		_raw = raw;
		Doc = doc;
		if (doc.TryGetValue("PlayerName", out BsonValue value) && value != null)
		{
			PlayerName = value.AsString();
		}
		if (doc.TryGetValue("_guid", out BsonValue value2) && value2 != null)
		{
			PlayerGuid = value2.AsString();
		}
	}

	public static (SaveFile? file, string error) Load(string path)
	{
		string text = ResolveSaveDir(path);
		if (!Directory.Exists(text))
		{
			return (file: null, error: "Directory not found: " + text);
		}
		if (!File.Exists(Path.Combine(text, "CURRENT")))
		{
			return (file: null, error: "No CURRENT file found in: " + text + "\nCheck the path points to a RocksDB player folder.");
		}
		PlayerSaveData playerSaveData = RocksDbAccess.ReadFromWal(text);
		if (playerSaveData == null)
		{
			playerSaveData = RocksDbAccess.ReadFromSstDirect(text);
		}
		if (playerSaveData == null)
		{
			playerSaveData = RocksDbAccess.ReadFromSst(text);
		}
		if (playerSaveData == null)
		{
			return (file: null, error: "Could not find player data in WAL or SST files.\n\n"
				+ "Try:\n"
				+ "1) Close the game completely (unlocks WAL / logs if present).\n"
				+ "2) Open the folder that contains CURRENT and *.sst (usually ...\\Players\\<32-char hex>).\n"
				+ "3) Copy rocksdb.dll from your game install next to WIM - Windrose Inventory Manager.exe, then Load again — "
				+ "SST-only saves need that DLL to read the database.\n"
				+ "4) If it still fails, pick a parent folder so the editor can search for Players\\<id>\\CURRENT.");
		}
		BsonDocument doc;
		try
		{
			doc = BsonParser.Parse(playerSaveData.BsonBytes);
		}
		catch (Exception ex)
		{
			return (file: null, error: "BSON parse error: " + ex.Message);
		}
		return (file: new SaveFile(playerSaveData, doc), error: "");
	}

	public List<ModuleInfo> GetModules()
	{
		List<ModuleInfo> list = new List<ModuleInfo>();
		BsonValue bsonValue = Doc.Navigate("Inventory.Modules");
		if (bsonValue == null || !bsonValue.IsDocument)
		{
			return list;
		}
		foreach (var (s, bsonValue3) in bsonValue.AsDocument())
		{
			if (bsonValue3.IsDocument)
			{
				BsonDocument mod = bsonValue3.AsDocument();
				int moduleCapacity = GetModuleCapacity(mod);
				int used = CountUsedSlots(mod);
				if (int.TryParse(s, out var result))
				{
					list.Add(new ModuleInfo
					{
						Index = result,
						Capacity = moduleCapacity,
						Used = used,
						Label = InferModuleLabel(result, mod)
					});
				}
			}
		}
		return list.OrderBy((ModuleInfo m) => m.Index).ToList();
	}

	private static string InferModuleLabel(int idx, BsonDocument mod)
	{
		int mc = GetModuleCapacity(mod);
		List<string> paths = new List<string>();
		if (mod.TryGetValue("Slots", out BsonValue value) && value != null && value.IsDocument)
		{
			foreach (var (_, bsonValue2) in value.AsDocument())
			{
				if (!bsonValue2.IsDocument)
				{
					continue;
				}
				BsonValue bsonValue3 = bsonValue2.AsDocument().Navigate("ItemsStack.Item.ItemParams");
				if (bsonValue3 == null || string.IsNullOrEmpty(bsonValue3.AsString()))
				{
					continue;
				}
				paths.Add(bsonValue3.AsString().ToLowerInvariant());
			}
		}
		if (paths.Count == 0)
		{
			return $"Mod {idx}";
		}
		bool Any(Func<string, bool> pred)
		{
			for (int i = 0; i < paths.Count; i++)
			{
				if (pred(paths[i]))
				{
					return true;
				}
			}
			return false;
		}

		if (Any((string p) => p.Contains("/ammo/")))
		{
			return $"Mod {idx}: Ammo";
		}
		if (Any((string p) => p.Contains("/armor/")))
		{
			return $"Mod {idx}: Armor";
		}
		if (Any((string p) => p.Contains("/weapon/")))
		{
			return $"Mod {idx}: Action Bar";
		}
		if (Any((string p) => p.Contains("/ring/") || p.Contains("/necklace/")))
		{
			return $"Mod {idx}: Accessories";
		}
		if (Any((string p) => p.Contains("/backpack/")))
		{
			return mc >= 10 ? $"Mod {idx}: Backpack" : $"Mod {idx}: Accessories";
		}
		if (Any((string p) => p.Contains("shipcustomization")))
		{
			return $"Mod {idx}: Ship Cosmetics";
		}
		if (Any((string p) => p.Contains("/npc/")))
		{
			return $"Mod {idx}: Crew";
		}
		for (int j = 0; j < paths.Count; j++)
		{
			string text2 = paths[j];
			if (text2.Contains("/resource/") || text2.Contains("/misc/"))
			{
				continue;
			}
			if (!text2.Contains("/alchemy/") && !text2.Contains("/food/"))
			{
				break;
			}
			return $"Mod {idx}: Action Bar";
		}
		return $"Mod {idx}";
	}

	public List<InventorySlot> GetSlots(int moduleIndex)
	{
		List<InventorySlot> list = new List<InventorySlot>();
		BsonValue bsonValue = Doc.Navigate("Inventory.Modules");
		if (bsonValue == null || !bsonValue.IsDocument)
		{
			return list;
		}
		if (!bsonValue.AsDocument().TryGetValue(moduleIndex.ToString(), out BsonValue value) || value == null || !value.IsDocument)
		{
			return list;
		}
		BsonDocument bsonDocument = value.AsDocument();
		int moduleCapacity = GetModuleCapacity(bsonDocument);
		BsonDocument bsonDocument2 = null;
		if (bsonDocument.TryGetValue("Slots", out BsonValue value2) && value2 != null && value2.IsDocument)
		{
			bsonDocument2 = value2.AsDocument();
		}
		Dictionary<int, BsonDocument> bsonSlotBySlotIndex = new Dictionary<int, BsonDocument>();
		int highestSlotIndex = -1;
		if (bsonDocument2 != null)
		{
			foreach (var (s, bsonValue3) in bsonDocument2)
			{
				if (!bsonValue3.IsDocument)
				{
					continue;
				}
				BsonDocument bsonDocumentForSlotEntry = bsonValue3.AsDocument();
				int rk = ResolveSlotDocIndexFromKeyAndBody(s, bsonDocumentForSlotEntry);
				if (rk < 0)
				{
					continue;
				}
				bsonSlotBySlotIndex[rk] = bsonDocumentForSlotEntry;
				highestSlotIndex = Math.Max(highestSlotIndex, rk);
			}
		}
		int slotCount = Math.Max(moduleCapacity, highestSlotIndex + 1);
		if (slotCount <= 0)
		{
			slotCount = 8;
		}
		for (int i = 0; i < slotCount; i++)
		{
			InventorySlot inventorySlot2 = new InventorySlot
			{
				ModuleIndex = moduleIndex,
				SlotIndex = i
			};
			if (bsonSlotBySlotIndex.TryGetValue(i, out BsonDocument slotDocEntry))
			{
				ReadSlotData(slotDocEntry, inventorySlot2);
			}
			list.Add(inventorySlot2);
		}
		return list;
	}

	private static void ReadSlotData(BsonDocument slotDoc, InventorySlot info)
	{
		if (!slotDoc.TryGetValue("ItemsStack", out BsonValue value) || value == null || !value.IsDocument)
		{
			return;
		}
		BsonDocument bsonDocument = value.AsDocument();
		if (bsonDocument.TryGetValue("Count", out BsonValue value2) && value2 != null)
		{
			info.Count = (int)value2.TryAsLong();
		}
		if (!bsonDocument.TryGetValue("Item", out BsonValue value3) || value3 == null || !value3.IsDocument)
		{
			return;
		}
		BsonDocument bsonDocument2 = value3.AsDocument();
		if (bsonDocument2.TryGetValue("ItemParams", out BsonValue value4) && value4 != null)
		{
			string p = (value4.AsString() ?? "").Trim();
			info.ItemParams = p.Length == 0 ? string.Empty : p;
		}
		if (bsonDocument2.TryGetValue("ItemId", out BsonValue value5) && value5 != null)
		{
			info.ItemId = value5.AsString();
		}
		if (bsonDocument2.TryGetValue("QualityLevel", out BsonValue value6) && value6 != null)
		{
			info.Quality = (int)value6.TryAsLong();
		}
		if (!bsonDocument2.TryGetValue("Attributes", out BsonValue value7) || value7 == null || !value7.IsDocument)
		{
			return;
		}
		foreach (var (_, bsonValue2) in value7.AsDocument())
		{
			if (!bsonValue2.IsDocument)
			{
				continue;
			}
			BsonDocument bsonDocument3 = bsonValue2.AsDocument();
			if (!bsonDocument3.TryGetValue("Tag", out BsonValue value8) || value8 == null || !value8.IsDocument || !value8.AsDocument().TryGetValue("TagName", out BsonValue value9) || value9 == null)
			{
				continue;
			}
			string text2 = value9.AsString();
			BsonValue value12;
			if (text2.Contains("Level") && !text2.Contains("Quality"))
			{
				if (bsonDocument3.TryGetValue("Value", out BsonValue value10) && value10 != null)
				{
					info.Level = (int)value10.TryAsLong();
				}
				if (bsonDocument3.TryGetValue("MaxValue", out BsonValue value11) && value11 != null)
				{
					info.MaxLevel = (int)value11.TryAsLong();
				}
			}
			else if (text2.Contains("Quality") && bsonDocument3.TryGetValue("Value", out value12) && value12 != null)
			{
				info.Quality = (int)value12.TryAsLong();
			}
		}
	}

	public bool AddItem(int moduleIndex, int slotIndex, string itemParams, int level, int count, int quality = 0)
	{
		BsonValue bsonValue = Doc.Navigate("Inventory.Modules");
		if (bsonValue == null || !bsonValue.IsDocument)
		{
			return false;
		}
		BsonDocument bsonDocument = bsonValue.AsDocument();
		string key = moduleIndex.ToString();
		if (!bsonDocument.TryGetValue(key, out BsonValue value) || value == null || !value.IsDocument)
		{
			return false;
		}
		BsonDocument bsonDocument2 = value.AsDocument();
		if (!bsonDocument2.ContainsKey("Slots"))
		{
			bsonDocument2["Slots"] = BsonValue.FromDocument(new BsonDocument());
		}
		BsonValue bsonValue2 = bsonDocument2["Slots"];
		if (!bsonValue2.IsDocument)
		{
			return false;
		}
		BsonDocument bsonDocument3 = bsonValue2.AsDocument();
		string v = Guid.NewGuid().ToString("N").ToUpper();
		BsonDocument bsonDocument4 = new BsonDocument();
		if (level > 0)
		{
			BsonDocument v2 = new BsonDocument { ["TagName"] = BsonValue.FromString("Inventory.Item.Attribute.Level") };
			bsonDocument4["0"] = BsonValue.FromDocument(new BsonDocument
			{
				["MaxValue"] = BsonValue.FromInt32(15),
				["Tag"] = BsonValue.FromDocument(v2),
				["Value"] = BsonValue.FromInt32(level)
			});
		}
		BsonDocument bsonDocument5 = new BsonDocument
		{
			["Attributes"] = BsonValue.FromArray(bsonDocument4),
			["Effects"] = BsonValue.FromArray(new BsonDocument()),
			["ItemId"] = BsonValue.FromString(v),
			["ItemParams"] = BsonValue.FromString(itemParams)
		};
		if (quality > 0)
		{
			bsonDocument5["QualityLevel"] = BsonValue.FromInt32(quality);
		}
		BsonDocument v3 = new BsonDocument
		{
			["Count"] = BsonValue.FromInt32(count),
			["Item"] = BsonValue.FromDocument(bsonDocument5)
		};
		BsonDocument v4 = new BsonDocument
		{
			["IsPersonalSlot"] = BsonValue.FromBool(v: false),
			["ItemsStack"] = BsonValue.FromDocument(v3),
			["SlotId"] = BsonValue.FromInt32(slotIndex),
			["SlotParams"] = BsonValue.FromString("/R5BusinessRules/Inventory/SlotsParams/DA_BL_Slot_Default.DA_BL_Slot_Default")
		};
		bsonDocument3[slotIndex.ToString()] = BsonValue.FromDocument(v4);
		IsModified = true;
		return true;
	}

	public bool EditItem(int moduleIndex, int slotIndex, int level, int count, int quality = 0)
	{
		BsonValue bsonValue = Doc.Navigate("Inventory.Modules");
		if (bsonValue == null || !bsonValue.IsDocument)
		{
			return false;
		}
		if (!bsonValue.AsDocument().TryGetValue(moduleIndex.ToString(), out BsonValue value) || value == null || !value.IsDocument)
		{
			return false;
		}
		if (!value.AsDocument().TryGetValue("Slots", out BsonValue value2) || value2 == null || !value2.IsDocument)
		{
			return false;
		}
		if (!value2.AsDocument().TryGetValue(slotIndex.ToString(), out BsonValue value3) || value3 == null || !value3.IsDocument)
		{
			return false;
		}
		if (!value3.AsDocument().TryGetValue("ItemsStack", out BsonValue value4) || value4 == null || !value4.IsDocument)
		{
			return false;
		}
		BsonDocument bsonDocument = value4.AsDocument();
		bsonDocument["Count"] = BsonValue.FromInt32(Math.Max(1, count));
		if (bsonDocument.TryGetValue("Item", out BsonValue value5) && value5 != null && value5.IsDocument)
		{
			BsonDocument bsonDocument2 = value5.AsDocument();
			if (quality > 0)
			{
				bsonDocument2["QualityLevel"] = BsonValue.FromInt32(quality);
			}
			if (bsonDocument2.TryGetValue("Attributes", out BsonValue value6) && value6 != null && value6.IsDocument)
			{
				foreach (var (_, bsonValue3) in value6.AsDocument())
				{
					if (!bsonValue3.IsDocument)
					{
						continue;
					}
					BsonDocument bsonDocument3 = bsonValue3.AsDocument();
					if (bsonDocument3.TryGetValue("Tag", out BsonValue value7) && value7 != null && value7.IsDocument && value7.AsDocument().TryGetValue("TagName", out BsonValue value8) && value8 != null)
					{
						string text2 = value8.AsString();
						if (text2.Contains("Level") && !text2.Contains("Quality"))
						{
							bsonDocument3["Value"] = BsonValue.FromInt32(level);
						}
						else if (text2.Contains("Quality") && quality > 0)
						{
							bsonDocument3["Value"] = BsonValue.FromInt32(quality);
						}
					}
				}
			}
		}
		IsModified = true;
		return true;
	}

	public bool RemoveItem(int moduleIndex, int slotIndex)
	{
		BsonValue bsonValue = Doc.Navigate("Inventory.Modules");
		if (bsonValue == null || !bsonValue.IsDocument)
		{
			return false;
		}
		if (!bsonValue.AsDocument().TryGetValue(moduleIndex.ToString(), out BsonValue value) || value == null || !value.IsDocument)
		{
			return false;
		}
		if (!value.AsDocument().TryGetValue("Slots", out BsonValue value2) || value2 == null || !value2.IsDocument)
		{
			return false;
		}
		value2.AsDocument().Remove(slotIndex.ToString());
		IsModified = true;
		return true;
	}

	public (bool ok, string error) Save()
	{
		byte[] bsonBytes;
		try
		{
			bsonBytes = BsonSerializer.Serialize(Doc);
		}
		catch (Exception ex)
		{
			return (ok: false, error: "Serialization error: " + ex.Message);
		}
		(long LastSeq, long NextFileNum, long LogNum) tuple = RocksDbAccess.ParseManifest(_raw.SaveDir);
		long item = tuple.LastSeq;
		long item2 = tuple.NextFileNum;
		long seq = ((_raw.Sequence > 0 && _raw.Sequence != 99999) ? _raw.Sequence : ((item > 0) ? item : 50000)) + 1;
		if (!RocksDbAccess.WriteWal(_raw.SaveDir, seq, item2, _raw.CfId, _raw.PlayerKey, bsonBytes))
		{
			return (ok: false, error: "Failed to write WAL file. Check permissions.");
		}
		IsModified = false;
		return (ok: true, error: "");
	}

	public string CreateBackup()
	{
		string text = _raw.SaveDir;
		string text2 = text;
		for (int i = 0; i < 6; i++)
		{
			string directoryName = Path.GetDirectoryName(text);
			if (directoryName == null || directoryName == text)
			{
				break;
			}
			string relativePath = Path.GetRelativePath(Path.GetPathRoot(directoryName) ?? "", directoryName);
			if (((!(relativePath == ".")) ? relativePath.Split(Path.DirectorySeparatorChar).Length : 0) < 2)
			{
				break;
			}
			text = directoryName;
			string text3 = Path.GetFileName(text) ?? "";
			if (text3.Length >= 10 && text3.All(char.IsDigit))
			{
				text2 = text;
				break;
			}
		}
		string text4 = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		string text5 = Path.GetFileName(text2);
		if (string.IsNullOrEmpty(text5))
		{
			text5 = "backup";
		}
		string text6 = Path.Combine(Path.GetDirectoryName(text2) ?? text2, text5 + "_backup_" + text4);
		try
		{
			int copied = 0;
			int skipped = 0;
			CopyDirectory(text2, text6, ref copied, ref skipped);
			string text7 = ((skipped > 0) ? $" ({skipped} file(s) skipped)" : "");
			return text6 + text7;
		}
		catch (Exception ex)
		{
			return "[BACKUP FAILED: " + ex.Message + "]";
		}
	}

	private static void CopyDirectory(string src, string dst, ref int copied, ref int skipped)
	{
		Directory.CreateDirectory(dst);
		string[] files = Directory.GetFiles(src);
		foreach (string text in files)
		{
			string fileName = Path.GetFileName(text);
			if (!(fileName == "LOCK") && !fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					File.Copy(text, Path.Combine(dst, fileName), overwrite: true);
					copied++;
				}
				catch
				{
					skipped++;
				}
			}
		}
		files = Directory.GetDirectories(src);
		foreach (string text2 in files)
		{
			try
			{
				CopyDirectory(text2, Path.Combine(dst, Path.GetFileName(text2)), ref copied, ref skipped);
			}
			catch
			{
				skipped++;
			}
		}
	}

	private static int GetModuleCapacity(BsonDocument mod)
	{
		int num = 0;
		if (mod.TryGetValue("AdditionalSlotsData", out BsonValue value) && value != null && value.IsDocument)
		{
			foreach (var (_, bsonValue2) in value.AsDocument())
			{
				if (bsonValue2.IsDocument && bsonValue2.AsDocument().TryGetValue("CountSlots", out BsonValue value2) && value2 != null)
				{
					num += (int)value2.TryAsLong();
				}
			}
		}
		if (mod.TryGetValue("ExtendCountSlots", out BsonValue value3) && value3 != null)
		{
			num += (int)value3.TryAsLong();
		}
		num = Math.Max(num, MultiplyPositiveDimensions(mod, "RowCount", "ColumnCount"));
		num = Math.Max(num, MultiplyPositiveDimensions(mod, "Rows", "Columns"));
		num = Math.Max(num, MultiplyPositiveDimensions(mod, "VerticalSlotAmount", "HorizontalSlotAmount"));
		num = Math.Max(num, MultiplyPositiveDimensions(mod, "SlotRows", "SlotColumns"));
		if (mod.TryGetValue("Slots", out BsonValue value4) && value4 != null && value4.IsDocument)
		{
			int mx = -1;
			foreach (var (key, bsonSlotVal) in value4.AsDocument())
			{
				if (!bsonSlotVal.IsDocument)
				{
					continue;
				}
				int resolved = ResolveSlotDocIndexFromKeyAndBody(key, bsonSlotVal.AsDocument());
				if (resolved >= 0)
				{
					mx = Math.Max(mx, resolved);
				}
			}
			if (mx >= 0)
			{
				num = Math.Max(num, mx + 1);
			}
		}
		foreach (string key in new[]
			{
				"TotalSlotCount", "TotalSlots", "SlotCount", "SlotsCount", "CountSlots",
				"BaseSlotCount", "DefaultSlotCount", "OpenSlotCount", "StorageSlotCount", "InventorySlotCount",
				"MaxSlots", "UnlockedSlotsCount"
			})
		{
			if (!mod.TryGetValue(key, out BsonValue tv) || tv == null)
			{
				continue;
			}
			long v = tv.TryAsLong();
			if (v > 0)
			{
				num = (int)Math.Max(num, v);
			}
		}
		if (mod.TryGetValue("MaxSlotIndex", out BsonValue mxi) && mxi != null)
		{
			long v = mxi.TryAsLong();
			if (v >= 0)
			{
				num = (int)Math.Max(num, v + 1);
			}
		}
		if (mod.TryGetValue("LastUnlockedSlotIndex", out BsonValue lus) && lus != null)
		{
			long v = lus.TryAsLong();
			if (v >= 0)
			{
				num = (int)Math.Max(num, v + 1);
			}
		}
		if (num <= 0)
		{
			return 8;
		}
		return num;
	}

	private static int MultiplyPositiveDimensions(BsonDocument mod, string rowsKey, string colsKey)
	{
		if (!mod.TryGetValue(rowsKey, out BsonValue a) || a == null)
		{
			return 0;
		}
		if (!mod.TryGetValue(colsKey, out BsonValue b) || b == null)
		{
			return 0;
		}
		long r = a.TryAsLong();
		long c = b.TryAsLong();
		if (r <= 0 || c <= 0)
		{
			return 0;
		}
		long p = r * c;
		if (p > int.MaxValue)
		{
			return int.MaxValue;
		}
		return (int)p;
	}

	private static int ResolveSlotDocIndexFromKeyAndBody(string bsonKeyIfAny, BsonDocument slotDoc)
	{
		if (int.TryParse(bsonKeyIfAny, out var fromKey))
		{
			return fromKey;
		}
		if (slotDoc.TryGetValue("SlotId", out BsonValue sid) && sid != null)
		{
			long v = sid.TryAsLong();
			if (v >= 0)
			{
				return v > int.MaxValue ? int.MaxValue : (int)v;
			}
		}
		if (slotDoc.TryGetValue("SlotIndex", out BsonValue six) && six != null)
		{
			long v2 = six.TryAsLong();
			if (v2 >= 0)
			{
				return v2 > int.MaxValue ? int.MaxValue : (int)v2;
			}
		}
		return -1;
	}

	private static int CountUsedSlots(BsonDocument mod)
	{
		if (!mod.TryGetValue("Slots", out BsonValue value) || value == null || !value.IsDocument)
		{
			return 0;
		}
		int num = 0;
		foreach (var (_, bsonValue2) in value.AsDocument())
		{
			if (bsonValue2.IsDocument)
			{
				BsonValue bsonValue3 = bsonValue2.AsDocument().Navigate("ItemsStack.Item.ItemParams");
				if (bsonValue3 != null && !string.IsNullOrEmpty(bsonValue3.AsString()))
				{
					num++;
				}
			}
		}
		return num;
	}

	private static string ResolveSaveDir(string path)
	{
		path = Path.GetFullPath(path);
		if (File.Exists(Path.Combine(path, "CURRENT")))
		{
			return path;
		}
		try
		{
			foreach (string item in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
			{
				if (Path.GetFileName(Path.GetDirectoryName(item) ?? "").Equals("Players", StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(item, "CURRENT")))
				{
					return item;
				}
			}
			foreach (string item2 in Directory.EnumerateFiles(path, "CURRENT", SearchOption.AllDirectories))
			{
				string directoryName = Path.GetDirectoryName(item2);
				if (Directory.GetFiles(directoryName, "*.log").Length != 0 || Directory.GetFiles(directoryName, "*.wal").Length != 0)
				{
					return directoryName;
				}
			}
		}
		catch
		{
		}
		return path;
	}
}
