using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace WIM;

public static class ItemDatabase
{
	private static readonly Regex ItemsRegex = new Regex("const ITEMS\\s*=\\s*(\\[[\\s\\S]*?\\]);", RegexOptions.Multiline);

	public static readonly string[] Rarities = new string[5] { "Legendary", "Epic", "Rare", "Uncommon", "Common" };

	public static List<ItemEntry> Items { get; private set; } = new List<ItemEntry>();

	public static bool Loaded { get; private set; }

	public static string LoadFromSqlite(string dbPath)
	{
		if (!File.Exists(dbPath))
		{
			return "File not found: " + dbPath;
		}
		try
		{
			using SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbPath + ";Mode=ReadOnly");
			sqliteConnection.Open();
			int num = 1;
			using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
			{
				sqliteCommand.CommandText = "SELECT value FROM meta WHERE key='version' LIMIT 1";
				if (sqliteCommand.ExecuteScalar() is string s && int.TryParse(s, out var result))
				{
					num = result;
				}
			}
			bool flag = num >= 2;
			Dictionary<long, ItemEntry> dictionary = new Dictionary<long, ItemEntry>();
			List<ItemEntry> list = new List<ItemEntry>();
			using (SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand())
			{
				sqliteCommand2.CommandText = (flag ? "SELECT id,filename,item_params_path,\n                                   display_name,   display_name_ru,\n                                   description,    description_ru,\n                                   vanity_text,    vanity_text_ru,\n                                   item_tag,item_type,rarity,category,icon_ref,\n                                   weight,keep_on_death,max_level,max_quality_level,max_count_in_slot\n                             FROM items WHERE item_params_path != '' ORDER BY id" : "SELECT id,filename,item_params_path,\n                                   display_name,'',description,'',vanity_text,'',\n                                   item_tag,item_type,rarity,category,icon_ref,\n                                   weight,keep_on_death,max_level,max_quality_level,max_count_in_slot\n                             FROM items WHERE item_params_path != '' ORDER BY id");
				using SqliteDataReader sqliteDataReader = sqliteCommand2.ExecuteReader();
				while (sqliteDataReader.Read())
				{
					long @int = sqliteDataReader.GetInt64(0);
					ItemEntry item = (dictionary[@int] = new ItemEntry
					{
						Filename = sqliteDataReader.GetString(1),
						ItemParamsPath = sqliteDataReader.GetString(2),
						DisplayNameEn = sqliteDataReader.GetString(3),
						DisplayNameRu = sqliteDataReader.GetString(4),
						DescriptionEn = sqliteDataReader.GetString(5),
						DescriptionRu = sqliteDataReader.GetString(6),
						VanityTextEn = sqliteDataReader.GetString(7),
						VanityTextRu = sqliteDataReader.GetString(8),
						ItemTag = sqliteDataReader.GetString(9),
						ItemType = sqliteDataReader.GetString(10),
						Rarity = sqliteDataReader.GetString(11),
						Category = sqliteDataReader.GetString(12),
						IconRef = sqliteDataReader.GetString(13),
						Weight = (float)sqliteDataReader.GetDouble(14),
						KeepOnDeath = (sqliteDataReader.GetInt32(15) != 0),
						MaxLevel = sqliteDataReader.GetInt32(16),
						MaxQualityLevel = sqliteDataReader.GetInt32(17),
						MaxCountInSlot = sqliteDataReader.GetInt32(18)
					});
					list.Add(item);
				}
			}
			using (SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand())
			{
				sqliteCommand3.CommandText = "\n                        SELECT item_id,kind,table_path,row_name,curve_level,stat_name\n                        FROM stat_curves\n                        ORDER BY item_id, kind, sort_order";
				using SqliteDataReader sqliteDataReader2 = sqliteCommand3.ExecuteReader();
				while (sqliteDataReader2.Read())
				{
					if (dictionary.TryGetValue(sqliteDataReader2.GetInt64(0), out var value))
					{
						string text = sqliteDataReader2.GetString(1);
						StatCurveRef statCurveRef = new StatCurveRef
						{
							Table = sqliteDataReader2.GetString(2),
							Row = sqliteDataReader2.GetString(3),
							Level = (float)sqliteDataReader2.GetDouble(4),
							Stat = sqliteDataReader2.GetString(5)
						};
						switch (text)
						{
						case "main":
							value.MainStatCurve = statCurveRef;
							break;
						case "secondary":
						{
							ItemEntry itemEntry2 = value;
							(itemEntry2.SecondaryStatCurves ?? (itemEntry2.SecondaryStatCurves = new List<StatCurveRef>())).Add(statCurveRef);
							break;
						}
						case "addl":
						{
							ItemEntry itemEntry2 = value;
							(itemEntry2.AddlStatCurves ?? (itemEntry2.AddlStatCurves = new List<StatCurveRef>())).Add(statCurveRef);
							break;
						}
						}
					}
				}
			}
			using (SqliteCommand sqliteCommand4 = sqliteConnection.CreateCommand())
			{
				sqliteCommand4.CommandText = "\n                        SELECT item_id,table_path,row_name,curve_level,display_type,inverse\n                        FROM desc_curves\n                        ORDER BY item_id, sort_order";
				using SqliteDataReader sqliteDataReader3 = sqliteCommand4.ExecuteReader();
				while (sqliteDataReader3.Read())
				{
					if (dictionary.TryGetValue(sqliteDataReader3.GetInt64(0), out var value2))
					{
						ItemEntry itemEntry2 = value2;
						(itemEntry2.DescCurves ?? (itemEntry2.DescCurves = new List<DescCurveRef>())).Add(new DescCurveRef
						{
							Table = sqliteDataReader3.GetString(1),
							Row = sqliteDataReader3.GetString(2),
							Level = (float)sqliteDataReader3.GetDouble(3),
							DisplayType = sqliteDataReader3.GetString(4),
							Inverse = (sqliteDataReader3.GetInt32(5) != 0)
						});
					}
				}
			}
			using (SqliteCommand sqliteCommand5 = sqliteConnection.CreateCommand())
			{
				sqliteCommand5.CommandText = (flag ? "SELECT item_id,text,text_ru FROM effects_texts ORDER BY item_id,sort_order" : "SELECT item_id,text,''      FROM effects_texts ORDER BY item_id,sort_order");
				using SqliteDataReader sqliteDataReader4 = sqliteCommand5.ExecuteReader();
				while (sqliteDataReader4.Read())
				{
					if (dictionary.TryGetValue(sqliteDataReader4.GetInt64(0), out var value3))
					{
						ItemEntry itemEntry2 = value3;
						(itemEntry2.EffectsTexts ?? (itemEntry2.EffectsTexts = new List<string>())).Add(sqliteDataReader4.GetString(1));
						itemEntry2 = value3;
						(itemEntry2.EffectsTextsRu ?? (itemEntry2.EffectsTextsRu = new List<string>())).Add(sqliteDataReader4.GetString(2));
					}
				}
			}
			using (SqliteCommand sqliteCommand6 = sqliteConnection.CreateCommand())
			{
				sqliteCommand6.CommandText = (flag ? "SELECT item_id,name,name_ru,description,description_ru,activation_count\n                             FROM set_effects ORDER BY item_id, sort_order" : "SELECT item_id,name,'',description,'',activation_count\n                             FROM set_effects ORDER BY item_id, sort_order");
				using SqliteDataReader sqliteDataReader5 = sqliteCommand6.ExecuteReader();
				while (sqliteDataReader5.Read())
				{
					if (dictionary.TryGetValue(sqliteDataReader5.GetInt64(0), out var value4))
					{
						ItemEntry itemEntry2 = value4;
						(itemEntry2.SetEffects ?? (itemEntry2.SetEffects = new List<SetEffectEntry>())).Add(new SetEffectEntry
						{
							NameEn = sqliteDataReader5.GetString(1),
							NameRu = sqliteDataReader5.GetString(2),
							DescriptionEn = sqliteDataReader5.GetString(3),
							DescriptionRu = sqliteDataReader5.GetString(4),
							ActivationCount = sqliteDataReader5.GetInt32(5)
						});
					}
				}
			}
			Items = list;
			Loaded = list.Count > 0;
			return "";
		}
		catch (Exception ex)
		{
			return "Error loading windrose_items.db: " + ex.Message;
		}
	}

	public static string LoadFromDb(string dbPath)
	{
		if (!File.Exists(dbPath))
		{
			return "File not found: " + dbPath;
		}
		try
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				AllowTrailingCommas = true
			};
			Items = JsonSerializer.Deserialize<List<ItemEntry>>(File.ReadAllText(dbPath, Encoding.UTF8), options)?.Where((ItemEntry i) => !string.IsNullOrEmpty(i.ItemParamsPath)).ToList() ?? new List<ItemEntry>();
			Loaded = Items.Count > 0;
			return "";
		}
		catch (Exception ex)
		{
			return "Error loading item_db.json: " + ex.Message;
		}
	}

	public static string Load(string htmlPath)
	{
		if (!File.Exists(htmlPath))
		{
			return "File not found: " + htmlPath;
		}
		string input;
		try
		{
			input = File.ReadAllText(htmlPath, Encoding.UTF8);
		}
		catch (Exception ex)
		{
			return "Read error: " + ex.Message;
		}
		Match match = ItemsRegex.Match(input);
		if (!match.Success)
		{
			return "Could not find ITEMS array in HTML file.";
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(match.Groups[1].Value);
			List<ItemEntry> list = new List<ItemEntry>();
			foreach (JsonElement item in jsonDocument.RootElement.EnumerateArray())
			{
				ItemEntry itemEntry = new ItemEntry
				{
					Filename = GetStr(item, "filename"),
					DisplayNameEn = GetStr(item, "display_name"),
					DescriptionEn = GetStr(item, "description"),
					VanityTextEn = GetStr(item, "vanity_text"),
					ItemTag = GetStr(item, "item_tag"),
					ItemType = GetStr(item, "item_type"),
					Rarity = GetStr(item, "rarity"),
					Category = GetStr(item, "category"),
					IconRef = GetStr(item, "icon_ref"),
					ItemParamsPath = GetStr(item, "item_params_path"),
					MainStat = GetStr(item, "main_stat"),
					KeepOnDeath = GetBool(item, "keep_on_death"),
					Weight = GetFloat(item, "weight"),
					MaxLevel = GetNullableInt(item, "max_level").GetValueOrDefault()
				};
				if (item.TryGetProperty("secondary_stats", out var value) && value.ValueKind == JsonValueKind.Array)
				{
					itemEntry.SecondaryStats = (from s in value.EnumerateArray()
						select s.GetString() ?? "" into s
						where s.Length > 0
						select s).ToList();
				}
				if (!string.IsNullOrEmpty(itemEntry.ItemParamsPath))
				{
					list.Add(itemEntry);
				}
			}
			Items = list;
			Loaded = true;
			return "";
		}
		catch (Exception ex2)
		{
			return "JSON parse error: " + ex2.Message;
		}
	}

	public static IEnumerable<ItemEntry> Filter(string search = "", string rarity = "", string category = "")
	{
		IEnumerable<ItemEntry> enumerable = Items;
		if (!string.IsNullOrWhiteSpace(search))
		{
			string s = search.Trim().ToLowerInvariant();
			enumerable = enumerable.Where((ItemEntry i) => i.DisplayName.ToLowerInvariant().Contains(s) || i.DisplayNameEn.ToLowerInvariant().Contains(s) || i.Filename.ToLowerInvariant().Contains(s) || i.ItemTag.ToLowerInvariant().Contains(s) || i.Category.ToLowerInvariant().Contains(s));
		}
		if (!string.IsNullOrEmpty(rarity))
		{
			enumerable = enumerable.Where((ItemEntry i) => i.Rarity.Equals(rarity, StringComparison.OrdinalIgnoreCase));
		}
		if (!string.IsNullOrEmpty(category) && category != "All")
		{
			enumerable = enumerable.Where((ItemEntry i) => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
		}
		return enumerable;
	}

	public static List<string> GetCategories()
	{
		return (from c in (from i in Items
				select i.Category into c
				where !string.IsNullOrEmpty(c)
				select c).Distinct()
			orderby c
			select c).ToList();
	}

	public static int RarityRank(string rarity)
	{
		return rarity switch
		{
			"Legendary" => 0, 
			"Epic" => 1, 
			"Rare" => 2, 
			"Uncommon" => 3, 
			"Common" => 4, 
			_ => 5, 
		};
	}

	public static string CategoryDisplayName(string cat)
	{
		return cat switch
		{
			"Armor" => "Armor",
			"MeleeWeapon" => "Melee weapon",
			"RangeWeapon" => "Ranged weapon",
			"Ammo" => "Ammo",
			"Ring" => "Ring",
			"Necklace" => "Necklace",
			"Backpack" => "Backpack",
			"Resource" => "Resource",
			"Misc" => "Misc",
			"Food" => "Food",
			"Alchemy" => "Alchemy",
			"Medicine" => "Medicine",
			"Tool" => "Tool",
			"Metal" => "Metal",
			"ShipWeapon" => "Ship weapon",
			"ShipHullMod" => "Hull mod",
			"ShipSailMod" => "Sail mod",
			"ShipCrewEquipment" => "Crew equipment",
			"ShipCombatOrder" => "Combat order",
			"Default" => "Default",
			_ => cat,
		};
	}

	private static string GetStr(JsonElement el, string key)
	{
		if (el.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
		{
			return value.GetString() ?? "";
		}
		return "";
	}

	private static bool GetBool(JsonElement el, string key)
	{
		if (el.TryGetProperty(key, out var value) && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False))
		{
			return value.GetBoolean();
		}
		return false;
	}

	private static float GetFloat(JsonElement el, string key)
	{
		if (el.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number)
		{
			return value.GetSingle();
		}
		return 0f;
	}

	private static int? GetNullableInt(JsonElement el, string key)
	{
		if (el.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number)
		{
			return value.GetInt32();
		}
		return null;
	}
}
