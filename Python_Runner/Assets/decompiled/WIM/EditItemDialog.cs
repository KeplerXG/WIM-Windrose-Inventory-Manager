using System;
using System.Drawing;
using System.Windows.Forms;

namespace WIM;

public class EditItemDialog : Form
{
	private static readonly Color BG = Theme.BG2;

	private static readonly Color BG2 = Theme.BG;

	private static readonly Color BG3 = Theme.SlotBg;

	private static readonly Color BORDER = Theme.SlotBord;

	private static readonly Color TEXT = Theme.Text;

	private static readonly Color DIM = Theme.Dim;

	private static readonly Color ACCENT = Theme.Accent;

	private readonly NumericUpDown _levelSpin;

	private readonly NumericUpDown _qualitySpin;

	private readonly NumericUpDown _countSpin;

	private readonly Button _applyBtn;

	public int SelectedLevel { get; private set; }

	public int SelectedQuality { get; private set; }

	public int SelectedCount { get; private set; }

	public EditItemDialog(InventorySlot slot, ItemEntry? item)
	{
		string text = item?.DisplayName ?? slot.InternalName;
		Text = "Edit  —  " + text;
		base.Size = new Size(420, 240);
		MinimumSize = new Size(360, 210);
		MaximumSize = new Size(560, 250);
		BackColor = BG;
		ForeColor = TEXT;
		Font = Theme.UiFont(9.5f);
		base.StartPosition = FormStartPosition.CenterParent;
		base.FormBorderStyle = FormBorderStyle.Sizable;
		int num = item?.MaxLevel ?? slot.MaxLevel;
		int num2 = item?.MaxQualityLevel ?? 0;
		int num3 = ((item != null && item.MaxCountInSlot > 0) ? item.MaxCountInSlot : 9999);
		SelectedLevel = ((slot.Level > 0) ? slot.Level : ((num > 0) ? 1 : 0));
		SelectedQuality = slot.Quality;
		SelectedCount = ((slot.Count <= 0) ? 1 : slot.Count);
		Panel hdr = new Panel
		{
			Dock = DockStyle.Top,
			Height = 46,
			BackColor = BG2
		};
		PictureBox pictureBox = new PictureBox
		{
			Size = new Size(36, 36),
			Location = new Point(8, 5),
			SizeMode = PictureBoxSizeMode.Zoom,
			BackColor = Theme.SlotBg
		};
		if (item != null)
		{
			pictureBox.Image = IconCache.Get(item.IconRef);
		}
		Label nameLbl = new Label
		{
			Text = text,
			Location = new Point(52, 6),
			AutoSize = false,
			Width = hdr.Width - 60,
			Height = 20,
			Font = Theme.UiFont(9.5f, FontStyle.Bold),
			ForeColor = TEXT,
			Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right)
		};
		Label rarLbl = new Label
		{
			Text = ((item != null) ? (item.Rarity + "  ·  " + ItemDatabase.CategoryDisplayName(item.Category)) : ""),
			Location = new Point(52, 26),
			AutoSize = false,
			Width = hdr.Width - 60,
			Height = 16,
			Font = Theme.UiFont(8f),
			ForeColor = ((item != null) ? Theme.Rarity(item.Rarity) : DIM),
			Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right)
		};
		hdr.Controls.AddRange(new Control[3] { pictureBox, nameLbl, rarLbl });
		hdr.Resize += delegate
		{
			nameLbl.Width = hdr.Width - 60;
			rarLbl.Width = hdr.Width - 60;
		};
		Panel panel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = BG,
			Padding = new Padding(16, 8, 16, 0)
		};
		bool flag = num > 0;
		Label control = MakeLbl($"Level (1–{(flag ? num : 0)}):");
		_levelSpin = new NumericUpDown
		{
			Minimum = (flag ? 1 : 0),
			Maximum = (flag ? num : 0),
			Value = (flag ? Math.Clamp(SelectedLevel, 1, num) : 0),
			Width = 70,
			BackColor = BG3,
			ForeColor = TEXT,
			Enabled = flag
		};
		bool flag2 = num2 > 0;
		Label label = MakeLbl($"Quality (0–{num2}):");
		_qualitySpin = new NumericUpDown
		{
			Minimum = 0m,
			Maximum = (flag2 ? num2 : 0),
			Value = (flag2 ? Math.Clamp(SelectedQuality, 0, num2) : 0),
			Width = 70,
			BackColor = BG3,
			ForeColor = TEXT,
			Enabled = flag2
		};
		label.Visible = flag2;
		_qualitySpin.Visible = flag2;
		Label control2 = MakeLbl($"Count (1–{num3}):");
		_countSpin = new NumericUpDown
		{
			Minimum = 1m,
			Maximum = num3,
			Value = Math.Clamp(SelectedCount, 1, num3),
			Width = 70,
			BackColor = BG3,
			ForeColor = TEXT
		};
		int columnCount = (flag2 ? 6 : 4);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Top,
			ColumnCount = columnCount,
			RowCount = 1,
			AutoSize = true,
			BackColor = BG
		};
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
		if (flag2)
		{
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
		}
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
		tableLayoutPanel.Margin = new Padding(0, 12, 0, 0);
		tableLayoutPanel.Controls.Add(control, 0, 0);
		tableLayoutPanel.Controls.Add(_levelSpin, 1, 0);
		if (flag2)
		{
			tableLayoutPanel.Controls.Add(label, 2, 0);
			tableLayoutPanel.Controls.Add(_qualitySpin, 3, 0);
			tableLayoutPanel.Controls.Add(control2, 4, 0);
			tableLayoutPanel.Controls.Add(_countSpin, 5, 0);
		}
		else
		{
			tableLayoutPanel.Controls.Add(control2, 2, 0);
			tableLayoutPanel.Controls.Add(_countSpin, 3, 0);
		}
		panel.Controls.Add(tableLayoutPanel);
		Panel bot = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 48,
			BackColor = BG2
		};
		_applyBtn = new Button
		{
			Text = "Apply",
			Width = 110,
			Height = 30,
			BackColor = Theme.AccentDeep,
			ForeColor = ACCENT,
			FlatStyle = FlatStyle.Flat,
			DialogResult = DialogResult.OK
		};
		_applyBtn.FlatAppearance.BorderColor = ACCENT;
		_applyBtn.Click += delegate
		{
			Accept();
		};
		Button cancelBtn = new Button
		{
			Text = "Cancel",
			Width = 90,
			Height = 30,
			BackColor = BG3,
			ForeColor = TEXT,
			FlatStyle = FlatStyle.Flat,
			DialogResult = DialogResult.Cancel
		};
		cancelBtn.FlatAppearance.BorderColor = BORDER;
		bot.Controls.Add(_applyBtn);
		bot.Controls.Add(cancelBtn);
		bot.Layout += delegate
		{
			int num4 = bot.ClientSize.Width - 10;
			_applyBtn.Location = new Point(num4 - _applyBtn.Width, 9);
			cancelBtn.Location = new Point(_applyBtn.Left - cancelBtn.Width - 8, 9);
		};
		base.Controls.Add(panel);
		base.Controls.Add(hdr);
		base.Controls.Add(bot);
		base.AcceptButton = _applyBtn;
		base.CancelButton = cancelBtn;
	}

	private void Accept()
	{
		SelectedLevel = (int)_levelSpin.Value;
		SelectedQuality = (int)_qualitySpin.Value;
		SelectedCount = (int)_countSpin.Value;
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private static Label MakeLbl(string text)
	{
		return new Label
		{
			Text = text,
			ForeColor = Theme.Dim,
			AutoSize = true,
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = new Padding(0, 7, 8, 0)
		};
	}
}
