using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WIM;

internal static class Program
{
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool SetDllDirectory(string lpPathName);

	[STAThread]
	private static void Main(string[] args)
	{
		try
		{
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			SetDllDirectory(baseDirectory);
			string fullPath = Path.GetFullPath(Path.Combine(baseDirectory, ".."));
			if (File.Exists(Path.Combine(fullPath, "rocksdb.dll")))
			{
				SetDllDirectory(fullPath);
			}
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(defaultValue: false);
			string? initialSave = ParseInitialSaveDirectory(args);
			MainForm mainForm = new MainForm(initialSave);
			AppIcon.ApplyTo(mainForm);
			Application.Run(mainForm);
		}
		catch (Exception ex)
		{
			try
			{
				MessageBox.Show(ex.ToString(), "WIM — startup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			catch
			{
			}
			Environment.ExitCode = 1;
		}
	}

	private static string? ParseInitialSaveDirectory(string[]? args)
	{
		if (args == null || args.Length == 0) return null;
		foreach (string raw in args)
		{
			if (string.IsNullOrWhiteSpace(raw)) continue;
			string a = raw.Trim();
			if (a.StartsWith("--save-path=", StringComparison.OrdinalIgnoreCase))
			{
				return StripQuotes(a.Substring("--save-path=".Length).Trim());
			}
		}
		foreach (string raw in args)
		{
			if (string.IsNullOrWhiteSpace(raw)) continue;
			string a = raw.Trim();
			if (a.StartsWith('-')) continue;
			return StripQuotes(a);
		}
		return null;
	}

	private static string StripQuotes(string s)
	{
		if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
		{
			return s.Substring(1, s.Length - 2);
		}
		return s;
	}
}
