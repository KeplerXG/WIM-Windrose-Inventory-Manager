using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WIM;

/// <summary>
/// Extracts an embedded windrose_items SQLite file so the editor runs without shipping the DB next to the exe.
/// </summary>
internal static class BundledItemDatabase
{
	private const string BundledResourceName = "windrose_items.bundled.db";

	public static bool TryGetExtractedPath(out string path)
	{
		path = "";
		Assembly asm = Assembly.GetExecutingAssembly();
		string? resName = asm.GetManifestResourceNames().FirstOrDefault((string n) =>
			string.Equals(n, BundledResourceName, StringComparison.OrdinalIgnoreCase)
			|| n.EndsWith("." + BundledResourceName, StringComparison.OrdinalIgnoreCase));
		if (resName == null)
		{
			return false;
		}
		using Stream? stream = asm.GetManifestResourceStream(resName);
		if (stream == null)
		{
			return false;
		}
		string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WIM-WindroseInventoryManager");
		Directory.CreateDirectory(dir);
		string dest = Path.Combine(dir, "windrose_items.db");
		using (FileStream fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.Read))
		{
			stream.CopyTo(fs);
		}
		path = dest;
		return true;
	}
}
