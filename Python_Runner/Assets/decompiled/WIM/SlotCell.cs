using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace WIM;

internal class SlotCell : Panel
{
	private readonly InventorySlot _slot;

	private readonly ItemEntry? _item;

	private readonly string _name;

	private readonly Color _rarCol;

	private readonly Image? _icon;

	private readonly string? _slotName;

	private readonly bool _addable;

	private readonly bool _showEmptyAddHint;

	private bool _hover;

	public event Action<InventorySlot>? OnAdd;

	public event Action<InventorySlot>? OnRemove;

	public event Action<InventorySlot>? OnEdit;

	public SlotCell(InventorySlot slot, ToolTip tip, string? slotName = null, bool addable = true, bool editable = true, bool showEmptyAddHint = true)
	{
		SlotCell slotCell = this;
		_slot = slot;
		_slotName = slotName;
		_addable = addable;
		_showEmptyAddHint = showEmptyAddHint;
		base.Size = new Size(Theme.SlotSz, Theme.SlotSz);
		DoubleBuffered = true;
		bool hasStack = !InventorySlot.IsVisuallyEmpty(slot);
		if (hasStack)
		{
			_item = ItemDatabase.Items.FirstOrDefault((ItemEntry i) => i.ItemParamsPath.Equals(slot.ItemParams, StringComparison.OrdinalIgnoreCase));
			_name = _item?.DisplayName ?? slot.InternalName;
			_rarCol = Theme.Rarity(_item?.Rarity ?? "");
			_icon = ((_item != null) ? IconCache.Get(_item.IconRef) : null);
			ContextMenuStrip contextMenuStrip = new ContextMenuStrip
			{
				BackColor = Theme.SlotBg,
				ForeColor = Theme.Text
			};
			if (editable)
			{
				contextMenuStrip.Items.Add("Edit").Click += delegate
				{
					slotCell.OnEdit?.Invoke(slotCell._slot);
				};
			}
			contextMenuStrip.Items.Add("Delete").Click += delegate
			{
				slotCell.OnRemove?.Invoke(slotCell._slot);
			};
			ContextMenuStrip = contextMenuStrip;
		}
		else
		{
			_name = "";
			_rarCol = Color.Empty;
			_item = null;
			if (addable)
			{
				Cursor = (showEmptyAddHint ? Cursors.Hand : Cursors.Default);
			}
			if (!string.IsNullOrWhiteSpace(slot.ItemParams))
			{
				ContextMenuStrip contextMenuStrip2 = new ContextMenuStrip
				{
					BackColor = Theme.SlotBg,
					ForeColor = Theme.Text
				};
				contextMenuStrip2.Items.Add("Delete").Click += delegate
				{
					slotCell.OnRemove?.Invoke(slotCell._slot);
				};
				ContextMenuStrip = contextMenuStrip2;
			}
		}
		base.MouseEnter += delegate
		{
			slotCell._hover = true;
			slotCell.Invalidate();
			if (hasStack)
			{
				Point screenPos = slotCell.PointToScreen(new Point(slotCell.Width, 0));
				ItemTooltip.Instance.ShowFor(slotCell._slot, slotCell._item, screenPos);
			}
		};
		base.MouseLeave += delegate
		{
			slotCell._hover = false;
			slotCell.Invalidate();
			ItemTooltip.Instance.Hide();
		};
		base.Click += delegate
		{
			if (InventorySlot.IsVisuallyEmpty(slotCell._slot) && slotCell._addable)
			{
				slotCell.OnAdd?.Invoke(slotCell._slot);
			}
		};
		base.DoubleClick += delegate
		{
			if (hasStack)
			{
				slotCell.OnEdit?.Invoke(slotCell._slot);
			}
		};
		base.Paint += OnPaint;
	}

	private void OnPaint(object? sender, PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.None;
		int num = Theme.SlotSz;
		int num2 = Theme.SlotSz;
		Color color = (_hover ? Theme.SlotBgHov : Theme.SlotBg);
		Color color2 = (_hover ? Theme.SlotBordH : Theme.SlotBord);
		using (SolidBrush brush = new SolidBrush(color))
		{
			graphics.FillRectangle(brush, 0, 0, num, num2);
		}
		using (Pen pen = new Pen(color2))
		{
			graphics.DrawRectangle(pen, 0, 0, num - 1, num2 - 1);
		}
		if (InventorySlot.IsVisuallyEmpty(_slot))
		{
			if (_slotName != null)
			{
				float slotFont = Math.Max(7.5f, num * 0.09f);
				using Font font = Theme.UiFont(slotFont);
				Rectangle slotLabelRect = new Rectangle(2, 2, num - 4, num2 - 4);
				TextRenderer.DrawText(graphics, _slotName, font, slotLabelRect, Color.FromArgb(_hover ? 80 : 45, Theme.Text), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
			}
			if (!_hover || !_addable || !_showEmptyAddHint)
			{
				return;
			}
			using SolidBrush brush3 = new SolidBrush(Color.FromArgb(130, Theme.Accent));
			using Font font2 = Theme.UiFont(Math.Max(16f, num * 0.22f), FontStyle.Bold);
			StringFormat format2 = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			graphics.DrawString("+", font2, brush3, new RectangleF(0f, 0f, num, num2), format2);
			return;
		}
		int bottomReserved = 5;
		int nameBlockH = Math.Max(26, Math.Min(42, (int)(num2 * 0.36f)));
		int topAvail = num2 - 2 - nameBlockH - bottomReserved;
		if (topAvail < 22)
		{
			nameBlockH = Math.Max(20, num2 - bottomReserved - 24);
			topAvail = num2 - 2 - nameBlockH - bottomReserved;
		}
		int num3 = Math.Min(Theme.SlotIconInner, Math.Max(20, topAvail - 2));
		int x = (num - num3) / 2;
		int y = 2 + Math.Max(0, (topAvail - num3) / 2);
		Rectangle rectangle = new Rectangle(x, y, num3, num3);
		using (SolidBrush brush4 = new SolidBrush(Color.FromArgb(35, _rarCol)))
		{
			graphics.FillRectangle(brush4, rectangle);
		}
		if (_icon != null)
		{
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.DrawImage(_icon, rectangle);
			graphics.SmoothingMode = SmoothingMode.None;
		}
		else
		{
			string s = ((_name.Length > 0) ? _name[0].ToString().ToUpper() : "?");
			using Font font3 = Theme.UiFont(Math.Max(12f, num3 * 0.24f), FontStyle.Bold);
			using SolidBrush brush5 = new SolidBrush(Color.FromArgb(200, _rarCol));
			StringFormat format3 = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			graphics.DrawString(s, font3, brush5, rectangle, format3);
		}
		if (_name.Length > 0)
		{
			string s2 = _name.Length > 80 ? _name.Substring(0, 80) + "…" : _name;
			float nameFontEm = Math.Max(7.75f, num * 0.088f);
			using Font font4 = Theme.UiFont(nameFontEm);
			int nameTop = num2 - bottomReserved - nameBlockH;
			Rectangle nameRect = new Rectangle(2, nameTop, num - 4, nameBlockH);
			Color nameCol = Color.FromArgb(210, Theme.Text);
			TextFormatFlags tf = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
			graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			TextRenderer.DrawText(graphics, s2, font4, nameRect, nameCol, tf);
			graphics.TextRenderingHint = TextRenderingHint.SystemDefault;
		}
		if (_slot.Count > 1)
		{
			string text = _slot.Count.ToString();
			using Font font5 = Theme.UiFont(Math.Max(7.5f, num * 0.092f), FontStyle.Bold);
			Size size = TextRenderer.MeasureText(text, font5);
			int num4 = num - size.Width - 1;
			int num5 = 1;
			using SolidBrush brush7 = new SolidBrush(Color.FromArgb(180, Theme.BG));
			graphics.FillRectangle(brush7, new Rectangle(num4 - 1, num5, size.Width + 2, size.Height));
			using SolidBrush brush8 = new SolidBrush(Theme.Text);
			graphics.DrawString(text, font5, brush8, new PointF(num4 - 1, num5));
		}
		using SolidBrush brush9 = new SolidBrush(Color.FromArgb(140, _rarCol));
		graphics.FillRectangle(brush9, 1, num2 - 3, num - 2, 2);
	}
}
