using System.Drawing;
using System.Windows.Forms;

namespace WIM;

/// <summary>
/// Windrose-style editor chrome: near-black charcoal, bronze/gold accents, serif titles (World Editor–like).
/// </summary>
internal static class Theme
{
	public static readonly Color BG = Color.FromArgb(15, 15, 15);

	public static readonly Color BG2 = Color.FromArgb(18, 18, 20);

	public static readonly Color InputBg = Color.FromArgb(12, 12, 14);

	public static readonly Color SlotBg = Color.FromArgb(22, 22, 24);

	public static readonly Color SlotBgHov = Color.FromArgb(34, 32, 30);

	public static readonly Color SlotBord = Color.FromArgb(58, 55, 52);

	public static readonly Color SlotBordH = Color.FromArgb(140, 118, 72);

	public static readonly Color HdrBg = Color.FromArgb(14, 14, 16);

	public static readonly Color HdrText = Color.FromArgb(197, 160, 89);

	public static readonly Color Text = Color.FromArgb(238, 235, 228);

	public static readonly Color Dim = Color.FromArgb(158, 152, 142);

	public static readonly Color Accent = Color.FromArgb(197, 160, 89);

	public static readonly Color AccentDeep = Color.FromArgb(10, 9, 8);

	public static readonly Color ProfileBand = Color.FromArgb(16, 15, 14);

	public static readonly Color Warn = Color.FromArgb(232, 120, 90);

	public static readonly Color SectionBorder = Color.FromArgb(88, 74, 52);

	public static readonly Color TabSelectedText = Color.FromArgb(12, 10, 8);

	public const int SlotSz = 62;

	public const int SlotGap = 5;

	public const int Pad = 10;

	public const int HdrH = 30;

	public static int CellPitch => SlotSz + SlotGap;

	public static int SlotIconInner => (SlotSz * 36 + 31) / 62;

	public static Font UiFont(float emSize, FontStyle style = FontStyle.Regular)
	{
		try
		{
			return new Font("Segoe UI", emSize, style, GraphicsUnit.Point);
		}
		catch
		{
			return new Font(SystemFonts.MessageBoxFont.FontFamily, emSize, style, GraphicsUnit.Point);
		}
	}

	public static Font TitleFont(float emSize, FontStyle style = FontStyle.Bold)
	{
		string[] faces = new string[3] { "Cambria", "Georgia", "Times New Roman" };
		foreach (string face in faces)
		{
			try
			{
				return new Font(face, emSize, style, GraphicsUnit.Point);
			}
			catch
			{
			}
		}
		return UiFont(emSize, style);
	}

	public static Color Rarity(string r)
	{
		return r switch
		{
			"Legendary" => Color.FromArgb(251, 166, 66),
			"Epic" => Color.FromArgb(196, 160, 255),
			"Rare" => Color.FromArgb(120, 190, 255),
			"Uncommon" => Color.FromArgb(110, 210, 150),
			"Common" => Color.FromArgb(175, 168, 158),
			_ => Color.FromArgb(130, 125, 118),
		};
	}

	public static int SecW(int cols)
	{
		return cols * SlotSz + (cols - 1) * SlotGap + 16;
	}
}
