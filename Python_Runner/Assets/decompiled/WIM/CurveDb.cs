using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace WIM;

public static class CurveDb
{
	private static Dictionary<string, Dictionary<string, float[][]>> _tables = new Dictionary<string, Dictionary<string, float[][]>>();

	public static bool Loaded { get; private set; }

	public static int TableCount { get; private set; }

	public static int RowCount { get; private set; }

	public static string LoadFromSqlite(string dbPath)
	{
		if (!File.Exists(dbPath))
		{
			return "";
		}
		try
		{
			using SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbPath + ";Mode=ReadOnly");
			sqliteConnection.Open();
			using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
			{
				sqliteCommand.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='curve_data'";
				if ((long)(sqliteCommand.ExecuteScalar() ?? ((object)0L)) == 0L)
				{
					return "";
				}
			}
			Dictionary<string, Dictionary<string, float[][]>> dictionary = new Dictionary<string, Dictionary<string, float[][]>>(StringComparer.OrdinalIgnoreCase);
			using SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand();
			sqliteCommand2.CommandText = "SELECT table_path, row_name, points FROM curve_data";
			using SqliteDataReader sqliteDataReader = sqliteCommand2.ExecuteReader();
			while (sqliteDataReader.Read())
			{
				string key = sqliteDataReader.GetString(0);
				string key2 = sqliteDataReader.GetString(1);
				float[][] value = UnpackCurvePoints(sqliteDataReader.GetFieldValue<byte[]>(2));
				if (!dictionary.TryGetValue(key, out var value2))
				{
					value2 = (dictionary[key] = new Dictionary<string, float[][]>(StringComparer.Ordinal));
				}
				value2[key2] = value;
			}
			_tables = dictionary;
			TableCount = dictionary.Count;
			RowCount = 0;
			foreach (Dictionary<string, float[][]> value3 in dictionary.Values)
			{
				RowCount += value3.Count;
			}
			Loaded = TableCount > 0;
			return "";
		}
		catch (Exception ex)
		{
			return "Error loading curves from SQLite: " + ex.Message;
		}
	}

	private static float[][] UnpackCurvePoints(byte[] blob)
	{
		int num = blob.Length / 8;
		float[][] array = new float[num][];
		for (int i = 0; i < num; i++)
		{
			array[i] = new float[2]
			{
				BitConverter.ToSingle(blob, i * 8),
				BitConverter.ToSingle(blob, i * 8 + 4)
			};
		}
		return array;
	}

	public static string Load(string curvesPath)
	{
		if (!File.Exists(curvesPath))
		{
			return "";
		}
		try
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			_tables = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, float[][]>>>(File.ReadAllText(curvesPath, Encoding.UTF8), options) ?? new Dictionary<string, Dictionary<string, float[][]>>();
			TableCount = _tables.Count;
			RowCount = 0;
			foreach (Dictionary<string, float[][]> value in _tables.Values)
			{
				RowCount += value.Count;
			}
			Loaded = TableCount > 0;
			return "";
		}
		catch (Exception ex)
		{
			return "Error loading curves_db.json: " + ex.Message;
		}
	}

	public static float? GetValue(string tablePath, string rowName, float level)
	{
		if (!_tables.TryGetValue(tablePath, out Dictionary<string, float[][]> value))
		{
			return null;
		}
		if (!value.TryGetValue(rowName, out var value2))
		{
			return null;
		}
		if (value2 == null || value2.Length == 0)
		{
			return null;
		}
		if (value2.Length == 1)
		{
			return value2[0][1];
		}
		for (int i = 0; i < value2.Length - 1; i++)
		{
			float num = value2[i][0];
			float num2 = value2[i][1];
			float num3 = value2[i + 1][0];
			float num4 = value2[i + 1][1];
			if (level <= num)
			{
				return num2;
			}
			if (level < num3)
			{
				float num5 = (level - num) / (num3 - num);
				return num2 + num5 * (num4 - num2);
			}
		}
		return value2[^1][1];
	}

	public static string FormatValue(float value, string displayType, bool inverse = false)
	{
		if (inverse)
		{
			value = 0f - value;
		}
		return displayType switch
		{
			"SecondsAsMinutes" => FormatDuration(value), 
			"RatioToPercent" => $"{value * 100f:0.#}%", 
			"ValueToPercent" => $"{value * 100f:+0.#;-0.#;0}%", 
			"ValueAsValue" => FormatNumber(value), 
			_ => FormatNumber(value), 
		};
	}

	public static string FormatNumber(float v)
	{
		if (Math.Abs((double)v - Math.Round(v)) < 0.004999999888241291)
		{
			return ((int)Math.Round(v)).ToString();
		}
		return v.ToString("0.##");
	}

	private static string FormatDuration(float seconds)
	{
		string value = "h";
		string value2 = "min";
		string value3 = "sec";
		if (seconds >= 3600f)
		{
			float num = seconds / 3600f;
			if ((double)num == Math.Floor(num))
			{
				return $"{(int)num} {value}";
			}
			return $"{num:0.#} {value}";
		}
		if (seconds >= 60f)
		{
			float num2 = seconds / 60f;
			if ((double)num2 == Math.Floor(num2))
			{
				return $"{(int)num2} {value2}";
			}
			return $"{num2:0.#} {value2}";
		}
		return $"{(int)seconds} {value3}";
	}

	public static string StatDisplayName(string stat)
	{
		return StatDisplayNameEn(stat);
	}

	private static string StatDisplayNameEn(string stat)
	{
		return stat switch
		{
			"Vitality" => "Vitality", 
			"Defence" => "Defence", 
			"AttackPower" => "Attack Power", 
			"Strength" => "Strength", 
			"Agility" => "Agility", 
			"Endurance" => "Endurance", 
			"Precision" => "Precision", 
			"Mastery" => "Mastery", 
			"Slash" => "Slash", 
			"Pierce" => "Pierce", 
			"Blunt" => "Blunt", 
			"Crude" => "Blunt", 
			"ShipCannonDamage" => "Damage", 
			"ShipCannonReloadTime" => "Reload", 
			"ShipFireAccuracyRANK" => "Accuracy", 
			"ShipFireRangeRANK" => "Range", 
			"None" => "", 
			_ => stat, 
		};
	}

	public static string StatSymbol(string stat)
	{
		return stat switch
		{
			"Vitality" => "♥", 
			"Defence" => "◈", 
			"AttackPower" => "⚔", 
			"Strength" => "◆", 
			"Agility" => "▲", 
			"Endurance" => "⬡", 
			"Precision" => "◎", 
			"Mastery" => "✦", 
			"Slash" => "⟋", 
			"Pierce" => "↑", 
			"Blunt" => "●", 
			"Crude" => "●", 
			"ShipCannonDamage" => "\ud83d\udca5", 
			"ShipCannonReloadTime" => "⏱", 
			"ShipFireAccuracyRANK" => "◎", 
			"ShipFireRangeRANK" => "↔", 
			_ => "•", 
		};
	}
}
