using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace WIM;

public sealed class ItemTooltip : Form
{
	private record StatLine(string Symbol, Color SymColor, string Value, string Name);

	private record EffectLine(string Text, Color Color);

	private record SetLine(string Header, Color HeaderColor, string Desc);

	private static ItemTooltip? _inst;

	private ItemEntry? _item;

	private InventorySlot? _slot;

	private const int W = 295;

	private const int Pad = 12;

	private const int IconSz = 52;

	private const int LH = 19;

	private const int SH = 15;

	private static readonly Color BgColor = Theme.BG2;

	private static readonly Color BorderColor = Theme.Accent;

	private static readonly Color TextColor = Theme.Text;

	private static readonly Color DimColor = Theme.Dim;

	private static readonly Color VanityColor = Color.FromArgb(188, 172, 148);

	private static readonly Color DescColor = Color.FromArgb(216, 208, 194);

	private static readonly Color SepColor = Theme.SectionBorder;

	private static readonly Color EffectColor = Color.FromArgb(130, 200, 155);

	private static readonly Color SetColor = Theme.Accent;

	private static readonly Font FntName = Theme.TitleFont(10f, FontStyle.Bold);

	private static readonly Font FntSub = Theme.UiFont(8f);

	private static readonly Font FntStat = Theme.UiFont(9f);

	private static readonly Font FntStatVal = Theme.UiFont(9f, FontStyle.Bold);

	private static readonly Font FntDesc = Theme.UiFont(8.5f);

	private static readonly Font FntVanity = Theme.UiFont(8f, FontStyle.Italic);

	private static readonly Font FntSet = Theme.UiFont(8.5f, FontStyle.Bold);

	private static readonly Font FntSetDesc = Theme.UiFont(8.5f);

	private static readonly Color DurColor = Color.FromArgb(130, 200, 130);

	public static ItemTooltip Instance
	{
		get
		{
			if (_inst == null || _inst.IsDisposed)
			{
				_inst = new ItemTooltip();
			}
			return _inst;
		}
	}

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 134217728;
			return createParams;
		}
	}

	private ItemTooltip()
	{
		base.FormBorderStyle = FormBorderStyle.None;
		base.ShowInTaskbar = false;
		base.TopMost = true;
		DoubleBuffered = true;
		BackColor = BgColor;
		base.StartPosition = FormStartPosition.Manual;
		base.Size = new Size(295, 100);
		base.Paint += OnPaint;
	}

	public void ShowFor(InventorySlot slot, ItemEntry? item, Point screenPos)
	{
		_slot = slot;
		_item = item;
		int num = ComputeHeight();
		base.Size = new Size(295, num);
		Rectangle workingArea = Screen.FromPoint(screenPos).WorkingArea;
		int num2 = screenPos.X + 14;
		int num3 = screenPos.Y + 4;
		if (num2 + 295 > workingArea.Right)
		{
			num2 = screenPos.X - 295 - 4;
		}
		if (num3 + num > workingArea.Bottom)
		{
			num3 = workingArea.Bottom - num - 4;
		}
		if (num2 < workingArea.Left)
		{
			num2 = workingArea.Left + 2;
		}
		if (num3 < workingArea.Top)
		{
			num3 = workingArea.Top + 2;
		}
		base.Location = new Point(num2, num3);
		Invalidate();
		if (!base.Visible)
		{
			Show();
		}
	}

	public new void Hide()
	{
		if (base.Visible)
		{
			base.Hide();
		}
	}

	private int ComputeHeight()
	{
		using Graphics g = Graphics.FromHwnd(IntPtr.Zero);
		int num = 70;
		if (_item == null)
		{
			return num + 27;
		}
		List<StatLine> list = BuildStatLines();
		string[] formattedVals;
		List<EffectLine> list2 = BuildEffectLines(out formattedVals);
		if (list.Count > 0)
		{
			num += 8;
			foreach (StatLine item in list)
			{
				_ = item;
				num += 19;
			}
		}
		bool flag = !string.IsNullOrEmpty(_item.Description);
		bool flag2 = !string.IsNullOrEmpty(_item.VanityText);
		bool flag3 = list2.Count > 0;
		bool flag4 = _item.SetEffects != null && _item.SetEffects.Count > 0;
		if (flag || flag2 || flag3 || flag4)
		{
			num += 8;
		}
		if (flag3)
		{
			foreach (EffectLine item2 in list2)
			{
				num += MeasureWrapped(g, item2.Text, 271, FntSetDesc) + 2;
			}
		}
		if (flag4)
		{
			foreach (SetLine item3 in BuildSetEffectLines())
			{
				num += 19;
				num += MeasureWrapped(g, item3.Desc, 261, FntSetDesc) + 2;
			}
		}
		if (flag)
		{
			string text = TryFormat(_item.Description, formattedVals);
			num += MeasureWrapped(g, text, 271, FntDesc) + 6;
		}
		if (flag2)
		{
			num += MeasureWrapped(g, _item.VanityText, 271, FntVanity) + 4;
		}
		num += 12;
		return Math.Max(num, 80);
	}

	private static int MeasureWrapped(Graphics g, string text, int width, Font font)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		return (int)Math.Ceiling(g.MeasureString(text, font, width, StringFormat.GenericTypographic).Height) + 2;
	}

	private List<StatLine> BuildStatLines()
	{
		List<StatLine> result = new List<StatLine>();
		if (_item == null)
		{
			return result;
		}
		int itemLevel = _slot?.Level ?? 0;
		AddRef(_item.MainStatCurve);
		if (_item.SecondaryStatCurves != null)
		{
			foreach (StatCurveRef secondaryStatCurf in _item.SecondaryStatCurves)
			{
				AddRef(secondaryStatCurf);
			}
		}
		if (_item.AddlStatCurves != null)
		{
			foreach (StatCurveRef addlStatCurf in _item.AddlStatCurves)
			{
				AddRef(addlStatCurf);
			}
		}
		return result;
		void AddRef(StatCurveRef? r)
		{
			if (r != null && !string.IsNullOrEmpty(r.Stat) && !(r.Stat == "None"))
			{
				string text = CurveDb.StatDisplayName(r.Stat);
				if (!string.IsNullOrEmpty(text))
				{
					float level = ((r.Level > 0f) ? r.Level : Math.Max(1f, itemLevel));
					string value = "";
					if (CurveDb.Loaded)
					{
						float? value2 = CurveDb.GetValue(r.Table, r.Row, level);
						if (value2.HasValue)
						{
							value = CurveDb.FormatNumber(value2.Value);
						}
					}
					result.Add(new StatLine(CurveDb.StatSymbol(r.Stat), StatColor(r.Stat), value, text));
				}
			}
		}
	}

	private List<EffectLine> BuildEffectLines(out string[] formattedVals)
	{
		formattedVals = Array.Empty<string>();
		List<EffectLine> list = new List<EffectLine>();
		if (_item == null)
		{
			return list;
		}
		int num = _slot?.Level ?? 0;
		List<string> list2 = new List<string>();
		if (_item.DescCurves != null)
		{
			foreach (DescCurveRef descCurf in _item.DescCurves)
			{
				float level = ((descCurf.Level > 0f) ? descCurf.Level : Math.Max(1f, num));
				float? num2 = (CurveDb.Loaded ? CurveDb.GetValue(descCurf.Table, descCurf.Row, level) : ((float?)null));
				list2.Add(num2.HasValue ? CurveDb.FormatValue(num2.Value, descCurf.DisplayType, descCurf.Inverse) : "?");
			}
			formattedVals = list2.ToArray();
		}
		List<string> effectsTextsLocalized = _item.EffectsTextsLocalized;
		bool num3 = effectsTextsLocalized != null && effectsTextsLocalized.Count > 0;
		bool flag = _item.SetEffects != null && _item.SetEffects.Count > 0;
		if (num3)
		{
			foreach (string item in effectsTextsLocalized)
			{
				string text = TryFormat(item, formattedVals);
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(new EffectLine(text, EffectColor));
				}
			}
		}
		else if (!flag && list2.Count > 0 && CurveDb.Loaded)
		{
			for (int i = 0; i < (_item.DescCurves?.Count ?? 0); i++)
			{
				DescCurveRef descCurveRef = _item.DescCurves[i];
				string text2 = list2[i];
				string text3 = ((descCurveRef.DisplayType == "SecondsAsMinutes") ? ("Duration: " + text2) : text2);
				if (!string.IsNullOrEmpty(text3))
				{
					list.Add(new EffectLine(text3, DurColor));
				}
			}
		}
		return list;
	}

	private List<SetLine> BuildSetEffectLines()
	{
		List<SetLine> list = new List<SetLine>();
		if (_item?.SetEffects == null)
		{
			return list;
		}
		BuildEffectLines(out string[] formattedVals);
		foreach (SetEffectEntry setEffect in _item.SetEffects)
		{
			string header = (string.IsNullOrEmpty(setEffect.Name) ? $"Set bonus (×{setEffect.ActivationCount})" : $"{setEffect.Name} (×{setEffect.ActivationCount})");
			string desc = TryFormat(setEffect.Description, formattedVals);
			list.Add(new SetLine(header, SetColor, desc));
		}
		return list;
	}

	private static string TryFormat(string template, string[] args)
	{
		if (string.IsNullOrEmpty(template))
		{
			return template;
		}
		if (args.Length == 0)
		{
			return template;
		}
		try
		{
			return string.Format(template, args.Cast<object>().ToArray());
		}
		catch
		{
			return template;
		}
	}

	private static Color StatColor(string stat)
	{
		switch (stat)
		{
		case "Vitality":
			return Color.FromArgb(255, 100, 100);
		case "Defence":
			return Color.FromArgb(100, 160, 255);
		case "AttackPower":
			return Color.FromArgb(255, 190, 60);
		case "Strength":
			return Color.FromArgb(255, 140, 60);
		case "Agility":
			return Color.FromArgb(100, 230, 130);
		case "Endurance":
			return Color.FromArgb(160, 130, 255);
		case "Precision":
			return Color.FromArgb(80, 200, 255);
		case "Mastery":
			return Color.FromArgb(255, 220, 80);
		case "Slash":
			return Color.FromArgb(220, 180, 100);
		case "Pierce":
			return Color.FromArgb(180, 220, 255);
		case "Blunt":
		case "Crude":
			return Color.FromArgb(200, 160, 120);
		default:
			return Color.FromArgb(190, 200, 210);
		}
	}

	private void OnPaint(object? sender, PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		int width = base.Width;
		int height = base.Height;
		using (SolidBrush brush = new SolidBrush(BgColor))
		{
			graphics.FillRectangle(brush, 0, 0, width, height);
		}
		using (Pen pen = new Pen(BorderColor))
		{
			graphics.DrawRectangle(pen, 0, 0, width - 1, height - 1);
		}
		Color color = ((_item != null) ? Theme.Rarity(_item.Rarity) : Theme.Dim);
		using (SolidBrush brush2 = new SolidBrush(color))
		{
			graphics.FillRectangle(brush2, 0, 0, width, 3);
		}
		int num = 12;
		Rectangle rectangle = new Rectangle(12, num, 52, 52);
		using (SolidBrush brush3 = new SolidBrush(Color.FromArgb(40, color)))
		{
			graphics.FillRectangle(brush3, rectangle);
		}
		using (Pen pen2 = new Pen(Color.FromArgb(80, color)))
		{
			graphics.DrawRectangle(pen2, rectangle);
		}
		Image image = ((_item != null) ? IconCache.Get(_item.IconRef) : null);
		if (image != null)
		{
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.DrawImage(image, rectangle);
			graphics.InterpolationMode = InterpolationMode.Default;
		}
		else if (_item != null)
		{
			string s = ((_item.DisplayName.Length > 0) ? _item.DisplayName.Substring(0, 1).ToUpper() : "?");
			using Font font = Theme.UiFont(16f, FontStyle.Bold);
			using SolidBrush brush4 = new SolidBrush(Color.FromArgb(180, color));
			StringFormat format = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			graphics.DrawString(s, font, brush4, rectangle, format);
		}
		int num2 = 72;
		int num3 = width - num2 - 12;
		if (_item != null)
		{
			using SolidBrush brush5 = new SolidBrush(color);
			graphics.DrawString(_item.DisplayName, FntName, brush5, new RectangleF(num2, num + 2, num3, 23f), new StringFormat
			{
				Trimming = StringTrimming.EllipsisCharacter
			});
		}
		else
		{
			using SolidBrush brush6 = new SolidBrush(DimColor);
			graphics.DrawString($"Slot {_slot?.SlotIndex} (empty)", FntDesc, brush6, num2, num + 8);
		}
		int num4 = num + 19 + 5;
		if (_item != null)
		{
			string text = _item.Rarity;
			string text2 = text;
			string text3 = ItemDatabase.CategoryDisplayName(_item.Category);
			string s2 = (string.IsNullOrEmpty(text3) ? text2 : (text2 + "  ·  " + text3));
			using SolidBrush brush7 = new SolidBrush(Color.FromArgb(170, color));
			graphics.DrawString(s2, FntSub, brush7, num2, num4);
		}
		string text4 = "";
		if (_slot != null && _slot.Level > 0)
		{
			text4 += $"Lv. {_slot.Level}";
		}
		if (_slot != null && _slot.Count > 1)
		{
			text4 = text4 + ((text4.Length > 0) ? "  " : "") + $"×{_slot.Count}";
		}
		if (text4.Length > 0)
		{
			using SolidBrush brush8 = new SolidBrush(DimColor);
			graphics.DrawString(text4, FntSub, brush8, num2, num4 + 15);
		}
		num = 70;
		if (_item == null)
		{
			return;
		}
		List<StatLine> list = BuildStatLines();
		if (list.Count > 0)
		{
			num += 2;
			using (Pen pen3 = new Pen(SepColor))
			{
				graphics.DrawLine(pen3, 12, num, width - 12, num);
			}
			num += 6;
			foreach (StatLine item in list)
			{
				using (SolidBrush brush9 = new SolidBrush(item.SymColor))
				{
					graphics.DrawString(item.Symbol, FntStatVal, brush9, 12f, num);
				}
				int num5 = 30;
				if (!string.IsNullOrEmpty(item.Value))
				{
					using (SolidBrush brush10 = new SolidBrush(TextColor))
					{
						graphics.DrawString(item.Value, FntStatVal, brush10, num5, num);
					}
					float width2 = graphics.MeasureString(item.Value + " ", FntStatVal).Width;
					using SolidBrush brush11 = new SolidBrush(DimColor);
					graphics.DrawString(item.Name, FntStat, brush11, (float)num5 + width2, num);
				}
				else
				{
					using SolidBrush brush12 = new SolidBrush(DimColor);
					graphics.DrawString(item.Name, FntStat, brush12, num5, num);
				}
				num += 19;
			}
		}
		string[] formattedVals;
		List<EffectLine> list2 = BuildEffectLines(out formattedVals);
		List<SetLine> list3 = BuildSetEffectLines();
		bool flag = !string.IsNullOrEmpty(_item.Description);
		bool flag2 = !string.IsNullOrEmpty(_item.VanityText);
		if (list2.Count > 0 || list3.Count > 0 || flag || flag2)
		{
			num += 4;
			using (Pen pen4 = new Pen(SepColor))
			{
				graphics.DrawLine(pen4, 12, num, width - 12, num);
			}
			num += 6;
		}
		foreach (EffectLine item2 in list2)
		{
			int num6 = DrawWrapped(graphics, item2.Text, 12, num, width - 24, FntSetDesc, item2.Color);
			num += num6 + 2;
		}
		foreach (SetLine item3 in list3)
		{
			using (SolidBrush brush13 = new SolidBrush(item3.HeaderColor))
			{
				graphics.DrawString("◈ " + item3.Header + ":", FntSet, brush13, 12f, num);
			}
			num += 19;
			int num7 = DrawWrapped(graphics, item3.Desc, 22, num, width - 24 - 10, FntSetDesc, TextColor);
			num += num7 + 2;
		}
		if (flag)
		{
			if (list2.Count > 0 || list3.Count > 0)
			{
				num += 2;
			}
			string text5 = TryFormat(_item.Description, formattedVals);
			int num8 = DrawWrapped(graphics, text5, 12, num, width - 24, FntDesc, DescColor);
			num += num8 + 4;
		}
		if (flag2)
		{
			DrawWrapped(graphics, _item.VanityText, 12, num, width - 24, FntVanity, VanityColor);
		}
	}

	private static int DrawWrapped(Graphics g, string text, int x, int y, int maxW, Font font, Color color)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		RectangleF layoutRectangle = new RectangleF(x, y, maxW, 2000f);
		using SolidBrush brush = new SolidBrush(color);
		g.DrawString(text, font, brush, layoutRectangle, StringFormat.GenericTypographic);
		return (int)Math.Ceiling(g.MeasureString(text, font, maxW, StringFormat.GenericTypographic).Height);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && _inst == this)
		{
			_inst = null;
		}
		base.Dispose(disposing);
	}
}
