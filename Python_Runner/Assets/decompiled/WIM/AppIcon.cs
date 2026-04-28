using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace WIM;

internal static class AppIcon
{
	private const string IcoFileName = "app.ico";

	private static MemoryStream? _embeddedIcoHold;

	public static void ApplyTo(Form form)
	{
		try
		{
			using Icon? ico = LoadIcon();
			if (ico == null)
			{
				return;
			}
			form.Icon = (Icon)ico.Clone();
		}
		catch
		{
		}
	}

	private static Icon? LoadIcon()
	{
		string disk = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, IcoFileName);
		if (File.Exists(disk))
		{
			return new Icon(disk);
		}
		Assembly asm = Assembly.GetExecutingAssembly();
		string? resName = asm.GetManifestResourceNames().FirstOrDefault((string n) => n.EndsWith(IcoFileName, StringComparison.OrdinalIgnoreCase));
		if (resName == null)
		{
			return null;
		}
		using Stream? stream = asm.GetManifestResourceStream(resName);
		if (stream == null)
		{
			return null;
		}
		_embeddedIcoHold = new MemoryStream();
		stream.CopyTo(_embeddedIcoHold);
		_embeddedIcoHold.Position = 0L;
		return new Icon(_embeddedIcoHold);
	}
}
