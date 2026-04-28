using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace WIM;

public class AddItemDialog : Form
{
	private static readonly Color BG = Theme.BG2;

	private static readonly Color BG2 = Theme.BG;

	private static readonly Color BG3 = Theme.SlotBg;

	private static readonly Color BORDER = Theme.SlotBord;

	private static readonly Color TEXT = Theme.Text;

	private static readonly Color ACCENT = Theme.Accent;

	private static readonly Color TEXTDIM = Theme.Dim;

	private TextBox _searchBox;

	private ComboBox _rarityBox;

	private ComboBox _catBox;

	private DataGridView _grid;

	private NumericUpDown _levelSpin;

	private NumericUpDown _qualitySpin;

	private NumericUpDown _countSpin;

	private Label _lvlLabel;

	private Label _qualLabel;

	private Label _cntLabel;

	private Label _infoLabel;

	private Button _addBtn;

	private List<ItemEntry> _filteredItems = new List<ItemEntry>();

	private Panel? bottomPanel;

	private readonly List<string> _catCodes = new List<string>();

	public ItemEntry? SelectedItem { get; private set; }

	public int SelectedLevel { get; private set; } = 1;

	public int SelectedQuality { get; private set; }

	public int SelectedCount { get; private set; } = 1;

	public AddItemDialog(int moduleIndex, int slotIndex)
	{
		Text = $"Add item  —  slot {slotIndex}";
		base.Size = new Size(900, 640);
		MinimumSize = new Size(700, 480);
		BackColor = BG;
		ForeColor = TEXT;
		Font = Theme.UiFont(9f);
		base.StartPosition = FormStartPosition.CenterParent;
		base.FormBorderStyle = FormBorderStyle.Sizable;
		BuildLayout();
		RebuildCatCodes();
		ApplyFilter();
	}

	private void BuildLayout()
	{
		Panel panel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 52,
			BackColor = BG2,
			Padding = new Padding(8, 8, 8, 8)
		};
		_searchBox = new TextBox
		{
			PlaceholderText = "Search by name, tag, category…",
			Width = 260,
			BackColor = BG3,
			ForeColor = TEXT,
			BorderStyle = BorderStyle.FixedSingle,
			Font = Theme.UiFont(9.5f)
		};
		_searchBox.TextChanged += delegate
		{
			ApplyFilter();
		};
		_rarityBox = CreateCombo(130);
		_rarityBox.Items.Add("All rarities");
		string[] rarities = ItemDatabase.Rarities;
		foreach (string item in rarities)
		{
			_rarityBox.Items.Add(item);
		}
		_rarityBox.SelectedIndex = 0;
		_rarityBox.SelectedIndexChanged += delegate
		{
			ApplyFilter();
		};
		_catBox = CreateCombo(170);
		_catBox.Items.Add("All");
		foreach (string category in ItemDatabase.GetCategories())
		{
			_catBox.Items.Add(ItemDatabase.CategoryDisplayName(category));
		}
		_catBox.SelectedIndex = 0;
		_catBox.SelectedIndexChanged += delegate
		{
			ApplyFilter();
		};
		Label label = MakeLabel("Search:");
		Label label2 = MakeLabel("Rarity:");
		Label label3 = MakeLabel("Category:");
		panel.Controls.AddRange(new Control[6] { label, _searchBox, label2, _rarityBox, label3, _catBox });
		int x = 8;
		LayoutRow(panel.Controls, ref x, 10, (label, 0), (_searchBox, 265), (label2, 0), (_rarityBox, 125), (label3, 0), (_catBox, 155));
		_grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			BackgroundColor = BG,
			GridColor = BORDER,
			BorderStyle = BorderStyle.None,
			RowHeadersVisible = false,
			AllowUserToAddRows = false,
			AllowUserToResizeRows = false,
			AllowUserToDeleteRows = false,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			MultiSelect = false,
			ReadOnly = true,
			ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
			ColumnHeadersHeight = 30,
			RowTemplate = 
			{
				Height = 42
			},
			DefaultCellStyle = 
			{
				BackColor = BG,
				ForeColor = TEXT,
				SelectionBackColor = BG2,
				SelectionForeColor = ACCENT
			},
			ColumnHeadersDefaultCellStyle = 
			{
				BackColor = BG3,
				ForeColor = TEXTDIM,
				Font = Theme.UiFont(8f, FontStyle.Bold)
			},
			AlternatingRowsDefaultCellStyle = 
			{
				BackColor = Theme.BG2
			},
			EnableHeadersVisualStyles = false
		};
		DataGridViewImageColumn dataGridViewColumn = new DataGridViewImageColumn
		{
			HeaderText = "",
			Width = 48,
			Resizable = DataGridViewTriState.False,
			ImageLayout = DataGridViewImageCellLayout.Zoom
		};
		_grid.Columns.Add(dataGridViewColumn);
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "Name",
			Width = 230
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "Rarity",
			Width = 90
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "Category",
			Width = 150
		});
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = "Max lv.",
			Width = 65
		});
		_grid.CellPainting += Grid_CellPainting;
		_grid.SelectionChanged += Grid_SelectionChanged;
		_grid.CellDoubleClick += delegate
		{
			AcceptItem();
		};
		bottomPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 68,
			BackColor = BG2,
			Padding = new Padding(12, 0, 12, 0)
		};
		_infoLabel = new Label
		{
			Text = "Select an item from the list",
			ForeColor = TEXTDIM,
			AutoSize = false,
			Width = 320,
			Height = 40,
			TextAlign = ContentAlignment.MiddleLeft,
			Font = Theme.UiFont(8.5f)
		};
		_lvlLabel = MakeLabel("Level:");
		_levelSpin = new NumericUpDown
		{
			Minimum = 0m,
			Maximum = 15m,
			Value = 1m,
			Width = 55,
			BackColor = BG3,
			ForeColor = TEXT
		};
		_qualLabel = MakeLabel("Quality:");
		_qualitySpin = new NumericUpDown
		{
			Minimum = 0m,
			Maximum = 0m,
			Value = 0m,
			Width = 55,
			BackColor = BG3,
			ForeColor = TEXT,
			Enabled = false,
			Visible = false
		};
		_qualLabel.Visible = false;
		_cntLabel = MakeLabel("Count:");
		_countSpin = new NumericUpDown
		{
			Minimum = 1m,
			Maximum = 9999m,
			Value = 1m,
			Width = 65,
			BackColor = BG3,
			ForeColor = TEXT
		};
		Button cancelBtn = new Button
		{
			Text = "Cancel",
			Width = 90,
			Height = 32,
			BackColor = BG3,
			ForeColor = TEXT,
			FlatStyle = FlatStyle.Flat,
			DialogResult = DialogResult.Cancel
		};
		cancelBtn.FlatAppearance.BorderColor = BORDER;
		_addBtn = new Button
		{
			Text = "Add",
			Width = 100,
			Height = 32,
			BackColor = Theme.AccentDeep,
			ForeColor = ACCENT,
			FlatStyle = FlatStyle.Flat,
			Enabled = false
		};
		_addBtn.FlatAppearance.BorderColor = ACCENT;
		_addBtn.Click += delegate
		{
			AcceptItem();
		};
		bottomPanel.Controls.AddRange(new Control[9] { _infoLabel, _lvlLabel, _levelSpin, _qualLabel, _qualitySpin, _cntLabel, _countSpin, cancelBtn, _addBtn });
		bottomPanel.Layout += delegate
		{
			_infoLabel.Location = new Point(12, 14);
			int num2 = bottomPanel.ClientSize.Width - 12;
			_addBtn.Location = new Point(num2 - _addBtn.Width, 18);
			cancelBtn.Location = new Point(_addBtn.Left - cancelBtn.Width - 8, 18);
			int num3 = cancelBtn.Left - 8;
			_countSpin.Location = new Point(num3 - _countSpin.Width, 22);
			num3 = _countSpin.Left;
			_cntLabel.Location = new Point(num3 - _cntLabel.Width - 4, 25);
			num3 = _cntLabel.Left;
			if (_qualitySpin.Visible)
			{
				_qualitySpin.Location = new Point(num3 - _qualitySpin.Width - 4, 22);
				num3 = _qualitySpin.Left;
				_qualLabel.Location = new Point(num3 - _qualLabel.Width - 4, 25);
				num3 = _qualLabel.Left;
			}
			_levelSpin.Location = new Point(num3 - _levelSpin.Width - 4, 22);
			num3 = _levelSpin.Left;
			_lvlLabel.Location = new Point(num3 - _lvlLabel.Width - 4, 25);
		};
		base.Controls.Add(_grid);
		base.Controls.Add(panel);
		base.Controls.Add(bottomPanel);
		base.AcceptButton = _addBtn;
		base.CancelButton = cancelBtn;
	}

	private void RebuildCatCodes()
	{
		_catCodes.Clear();
		_catCodes.Add("");
		foreach (string category in ItemDatabase.GetCategories())
		{
			_catCodes.Add(category);
		}
	}

	private void ApplyFilter()
	{
		string text = _searchBox.Text;
		string rarity = ((_rarityBox.SelectedIndex <= 0) ? "" : (_rarityBox.SelectedItem?.ToString() ?? ""));
		string category = "";
		int selectedIndex = _catBox.SelectedIndex;
		if (selectedIndex > 0 && selectedIndex < _catCodes.Count)
		{
			category = _catCodes[selectedIndex];
		}
		_filteredItems = ItemDatabase.Filter(text, rarity, category).ToList();
		PopulateGrid();
	}

	private void PopulateGrid()
	{
		_grid.Rows.Clear();
		foreach (ItemEntry filteredItem in _filteredItems)
		{
			int index = _grid.Rows.Add();
			DataGridViewRow dataGridViewRow = _grid.Rows[index];
			dataGridViewRow.Cells[0].Value = new Bitmap(1, 1);
			dataGridViewRow.Cells[1].Value = filteredItem.DisplayName;
			dataGridViewRow.Cells[2].Value = filteredItem.Rarity;
			dataGridViewRow.Cells[3].Value = ItemDatabase.CategoryDisplayName(filteredItem.Category);
			dataGridViewRow.Cells[4].Value = ((filteredItem.MaxLevel == 0) ? "—" : filteredItem.MaxLevel.ToString());
			dataGridViewRow.Tag = filteredItem;
		}
		_infoLabel.Text = $"{_filteredItems.Count} items";
		_addBtn.Enabled = false;
	}

	private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
	{
		if (e.ColumnIndex != 0 || e.RowIndex < 0)
		{
			return;
		}
		e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
		if (!(_grid.Rows[e.RowIndex].Tag is ItemEntry itemEntry) || e.Graphics == null)
		{
			e.Handled = true;
			return;
		}
		Color color = RarityColor(itemEntry.Rarity);
		int num = 34;
		Rectangle rectangle = new Rectangle(e.CellBounds.X + (e.CellBounds.Width - num) / 2, e.CellBounds.Y + (e.CellBounds.Height - num) / 2, num, num);
		using SolidBrush brush = new SolidBrush(Color.FromArgb(50, color));
		e.Graphics.FillRectangle(brush, rectangle);
		Image image = (string.IsNullOrEmpty(itemEntry.IconRef) ? null : IconCache.Get(itemEntry.IconRef));
		if (image != null)
		{
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			e.Graphics.DrawImage(image, rectangle);
		}
		else
		{
			using Pen pen = new Pen(Color.FromArgb(160, color), 1.5f);
			e.Graphics.DrawRectangle(pen, rectangle);
			string s = ((itemEntry.DisplayName.Length > 0) ? itemEntry.DisplayName[0].ToString().ToUpper() : "?");
			using Font font = Theme.UiFont(10f, FontStyle.Bold);
			using SolidBrush brush2 = new SolidBrush(color);
			StringFormat format = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			e.Graphics.DrawString(s, font, brush2, rectangle, format);
		}
		e.Handled = true;
	}

	private void Grid_SelectionChanged(object? sender, EventArgs e)
	{
		if (_grid.SelectedRows.Count == 0)
		{
			_addBtn.Enabled = false;
			return;
		}
		if (!(_grid.SelectedRows[0].Tag is ItemEntry itemEntry))
		{
			_addBtn.Enabled = false;
			return;
		}
		_addBtn.Enabled = true;
		_infoLabel.Text = itemEntry.DisplayName + "  ·  " + itemEntry.Rarity;
		if (!string.IsNullOrEmpty(itemEntry.Description))
		{
			Label infoLabel = _infoLabel;
			infoLabel.Text = infoLabel.Text + "\n" + TruncateAt(itemEntry.Description, 80);
		}
		int maxLevel = itemEntry.MaxLevel;
		_levelSpin.Maximum = ((maxLevel > 0) ? maxLevel : 0);
		if (maxLevel == 0)
		{
			_levelSpin.Value = 0m;
			_levelSpin.Enabled = false;
		}
		else
		{
			_levelSpin.Enabled = true;
			if (_levelSpin.Value < 1m)
			{
				_levelSpin.Value = 1m;
			}
		}
		bool flag = itemEntry.MaxQualityLevel > 0;
		_qualitySpin.Maximum = itemEntry.MaxQualityLevel;
		_qualitySpin.Value = 0m;
		_qualitySpin.Enabled = flag;
		_qualitySpin.Visible = flag;
		_qualLabel.Visible = flag;
		int num = ((itemEntry.MaxCountInSlot > 0) ? itemEntry.MaxCountInSlot : 9999);
		_countSpin.Maximum = num;
		if (_countSpin.Value > (decimal)num)
		{
			_countSpin.Value = num;
		}
		_cntLabel.Text = $"Count (1–{num}):";
		bottomPanel?.PerformLayout();
	}

	private void AcceptItem()
	{
		if (_grid.SelectedRows.Count != 0)
		{
			SelectedItem = _grid.SelectedRows[0].Tag as ItemEntry;
			if (SelectedItem != null)
			{
				SelectedLevel = (int)_levelSpin.Value;
				SelectedQuality = (int)_qualitySpin.Value;
				SelectedCount = (int)_countSpin.Value;
				base.DialogResult = DialogResult.OK;
				Close();
			}
		}
	}

	private static Color RarityColor(string rarity)
	{
		return rarity switch
		{
			"Legendary" => Color.FromArgb(249, 115, 22), 
			"Epic" => Color.FromArgb(168, 85, 247), 
			"Rare" => Color.FromArgb(59, 130, 246), 
			"Uncommon" => Color.FromArgb(34, 197, 94), 
			"Common" => Color.FromArgb(148, 163, 184), 
			_ => Color.FromArgb(100, 100, 100), 
		};
	}

	private static Bitmap GetRarityIcon(string rarity)
	{
		return new Bitmap(1, 1);
	}

	private static string TruncateAt(string s, int max)
	{
		if (s.Length > max)
		{
			return s.Substring(0, max) + "…";
		}
		return s;
	}

	private static Label MakeLabel(string text)
	{
		return new Label
		{
			Text = text,
			ForeColor = TEXTDIM,
			AutoSize = true,
			TextAlign = ContentAlignment.MiddleLeft
		};
	}

	private static ComboBox CreateCombo(int width)
	{
		return new ComboBox
		{
			Width = width,
			DropDownStyle = ComboBoxStyle.DropDownList,
			BackColor = BG3,
			ForeColor = TEXT,
			FlatStyle = FlatStyle.Flat
		};
	}

	private static void LayoutRow(Control.ControlCollection ctrls, ref int x, int y, params (Control ctrl, int advance)[] items)
	{
		for (int i = 0; i < items.Length; i++)
		{
			(Control ctrl, int advance) tuple = items[i];
			Control item = tuple.ctrl;
			int item2 = tuple.advance;
			item.Location = new Point(x, y + (30 - item.Height) / 2);
			x += ((item2 > 0) ? item2 : item.Width) + 8;
		}
	}
}
