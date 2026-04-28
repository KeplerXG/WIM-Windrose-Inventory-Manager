using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace WIM;

public static class IconCache
{
	private static string _iconDir = "";

	private static Dictionary<string, string> _map = new Dictionary<string, string>();

	private static SqliteConnection? _conn;

	private static readonly HashSet<string> _sqliteRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Image?> _cache = new Dictionary<string, Image>();

	public static bool HasIcons
	{
		get
		{
			if (_sqliteRefs.Count <= 0 && _map.Count <= 0)
			{
				return !string.IsNullOrEmpty(_iconDir);
			}
			return true;
		}
	}

	public static int MappedCount
	{
		get
		{
			if (_sqliteRefs.Count <= 0)
			{
				return _map.Count;
			}
			return _sqliteRefs.Count;
		}
	}

	public static void LoadFromSqlite(string dbPath)
	{
		_conn?.Close();
		_conn = null;
		_sqliteRefs.Clear();
		_cache.Clear();
		_map.Clear();
		_iconDir = "";
		if (!File.Exists(dbPath))
		{
			return;
		}
		try
		{
			_conn = new SqliteConnection("Data Source=" + dbPath + ";Mode=ReadOnly");
			_conn.Open();
			using SqliteCommand sqliteCommand = _conn.CreateCommand();
			sqliteCommand.CommandText = "SELECT icon_ref FROM icons";
			using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
			while (sqliteDataReader.Read())
			{
				_sqliteRefs.Add(sqliteDataReader.GetString(0));
			}
		}
		catch
		{
			_conn?.Close();
			_conn = null;
		}
	}

	public static void Load(string editorBaseDir)
	{
		_conn?.Close();
		_conn = null;
		_sqliteRefs.Clear();
		_cache.Clear();
		_map.Clear();
		_iconDir = Path.Combine(editorBaseDir, "icons");
		string path = Path.Combine(editorBaseDir, "icon_map.json");
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(path));
			foreach (JsonProperty item in jsonDocument.RootElement.EnumerateObject())
			{
				string text = item.Value.GetString();
				if (text != null)
				{
					_map[item.Name] = Path.Combine(editorBaseDir, text.Replace('/', Path.DirectorySeparatorChar));
				}
			}
		}
		catch
		{
		}
	}

	public static Image? Get(string iconRef)
	{
		if (string.IsNullOrEmpty(iconRef))
		{
			return null;
		}
		if (_cache.TryGetValue(iconRef, out Image value))
		{
			return value;
		}
		Image image = null;
		if (_conn != null && _sqliteRefs.Contains(iconRef))
		{
			try
			{
				using SqliteCommand sqliteCommand = _conn.CreateCommand();
				sqliteCommand.CommandText = "SELECT png_data FROM icons WHERE icon_ref = @ref LIMIT 1";
				sqliteCommand.Parameters.AddWithValue("@ref", iconRef);
				using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
				if (sqliteDataReader.Read() && !sqliteDataReader.IsDBNull(0))
				{
					using MemoryStream stream = new MemoryStream(sqliteDataReader.GetFieldValue<byte[]>(0));
					image = Image.FromStream(stream);
				}
			}
			catch
			{
				image = null;
			}
		}
		if (image == null)
		{
			string text = ResolveFilePath(iconRef);
			if (text != null && File.Exists(text))
			{
				try
				{
					using MemoryStream stream2 = new MemoryStream(File.ReadAllBytes(text));
					image = Image.FromStream(stream2);
				}
				catch
				{
					image = null;
				}
			}
		}
		_cache[iconRef] = image;
		return image;
	}

	private static string? ResolveFilePath(string iconRef)
	{
		if (_map.TryGetValue(iconRef, out string value))
		{
			return value;
		}
		if (!string.IsNullOrEmpty(_iconDir))
		{
			string text = iconRef.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
			string text2 = Path.Combine(_iconDir, text + ".png");
			if (File.Exists(text2))
			{
				return text2;
			}
			string text3;
			if (!text.StartsWith("Game" + Path.DirectorySeparatorChar))
			{
				text3 = text;
			}
			else
			{
				string text4 = text;
				text3 = text4.Substring(5, text4.Length - 5);
			}
			string text5 = text3;
			string text6 = Path.Combine(_iconDir, text5 + ".png");
			if (File.Exists(text6))
			{
				return text6;
			}
		}
		return null;
	}
}
