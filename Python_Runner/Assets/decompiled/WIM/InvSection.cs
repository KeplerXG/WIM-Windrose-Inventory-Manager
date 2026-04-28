using System;
using System.Drawing;
using System.Windows.Forms;

namespace WIM;

internal class InvSection : Panel
{
	private readonly Label _lbl;

	private Panel? _grid;

	private int _cols;

	private readonly bool _showHeader;

	public string Title
	{
		get
		{
			return _lbl.Text;
		}
		set
		{
			_lbl.Text = value;
			if (_showHeader)
			{
				_lbl.Visible = !string.IsNullOrEmpty(value);
				_lbl.Height = (_lbl.Visible ? Theme.HdrH : 0);
			}
		}
	}

	public InvSection(string title, bool showHeader = true)
	{
		_showHeader = showHeader;
		AutoSize = true;
		AutoSizeMode = AutoSizeMode.GrowAndShrink;
		BackColor = Theme.BG2;
		base.Padding = new Padding(0);
		_lbl = new Label
		{
			Text = title,
			Dock = DockStyle.Top,
			Height = (showHeader ? Theme.HdrH : 0),
			Visible = showHeader,
			BackColor = Theme.HdrBg,
			ForeColor = Theme.HdrText,
			Font = Theme.TitleFont(10f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Padding = new Padding(12, 0, 0, 0)
		};
		base.Controls.Add(_lbl);
	}

	private int HeaderHeight()
	{
		if (!_showHeader)
		{
			return 0;
		}
		if (!_lbl.Visible || string.IsNullOrEmpty(_lbl.Text))
		{
			return 0;
		}
		return Theme.HdrH;
	}

	public void SetSlots(InventorySlot[] slots, int cols, ToolTip tip, Action<InventorySlot>? onAdd, Action<InventorySlot> onRemove, Action<InventorySlot>? onEdit = null, Func<int, string?>? getSlotName = null, bool showEmptyAddHint = true)
	{
		_cols = cols;
		if (_grid != null)
		{
			base.Controls.Remove(_grid);
			_grid.Dispose();
		}
		_grid = new Panel
		{
			BackColor = Theme.BG2
		};
		int pitch = Theme.CellPitch;
		for (int i = 0; i < slots.Length; i++)
		{
			int num2 = i % cols;
			int num3 = i / cols;
			string slotName = getSlotName?.Invoke(slots[i].SlotIndex);
			SlotCell slotCell = new SlotCell(slots[i], tip, slotName, onAdd != null, onEdit != null, showEmptyAddHint);
			slotCell.Location = new Point(8 + num2 * pitch, 8 + num3 * pitch);
			if (onAdd != null)
			{
				slotCell.OnAdd += onAdd;
			}
			slotCell.OnRemove += onRemove;
			if (onEdit != null)
			{
				slotCell.OnEdit += onEdit;
			}
			_grid.Controls.Add(slotCell);
		}
		int num4 = ((slots.Length == 0) ? 1 : ((int)Math.Ceiling((double)slots.Length / (double)cols)));
		_grid.Width = 16 + cols * Theme.SlotSz + (cols - 1) * Theme.SlotGap;
		_grid.Height = 16 + num4 * Theme.SlotSz + (num4 - 1) * Theme.SlotGap;
		int hh = HeaderHeight();
		_grid.Location = new Point(0, hh);
		base.Controls.Add(_grid);
		base.Width = _grid.Width;
		base.Height = hh + _grid.Height;
	}

	/// <summary>
	/// Turn off AutoSize and widen the section so the slot grid is not clipped when hosted in a narrow column.
	/// </summary>
	internal void FitOuterWidth(int outerWidth)
	{
		if (outerWidth < 8)
		{
			return;
		}
		int minW = 40;
		if (_grid != null)
		{
			minW = _grid.Width;
		}
		else if (base.Width > 0)
		{
			minW = base.Width;
		}
		AutoSize = false;
		base.Width = Math.Max(minW, outerWidth);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
	}
}
