using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WIM;

public class MainForm : Form
{
	private TextBox _pathBox;
	private Button _loadBtn;
	private Button _saveBtn;
	private Button _creditsBtn;
	private Label _statusLbl;
	private Label _creditsLbl;
	private InvSection _actionBarSec;
	private InvSection _backpackSec;
	private InvSection _equipSec;
	private InvSection _accessorySec;
	private InvSection _ammoSec;
	private Panel _profileRow;
	private Label _profileCaptionLbl;
	private Label _profileNameLbl;
	private Panel _workPanel;
	private TabControl _invTabs;
	private SaveFile? _save;
	private readonly ToolTip _tip = new ToolTip { AutoPopDelay = 6000, InitialDelay = 400 };

	private int _modBackpack;
	private int _modActionBar = 3;
	private int _modArmor = 2;
	private int _modAccessory = 4;
	private int _modAmmo = 1;

	public MainForm() : this(null)
	{
	}

	public MainForm(string? initialSaveDirectory)
	{
		Text = Credits.WindowTitle;
		Size = new Size(1000, 750);
		MinimumSize = new Size(820, 560);
		BackColor = Theme.BG2;
		ForeColor = Theme.Text;
		Font = Theme.UiFont(9f);
		StartPosition = FormStartPosition.CenterScreen;
		Build();
		TryAutoLoadDb();
		if (!string.IsNullOrWhiteSpace(initialSaveDirectory))
		{
			_pathBox.Text = initialSaveDirectory.Trim();
			Shown += OnShownInitialLoad;
		}
	}

	private void OnShownInitialLoad(object? sender, EventArgs e)
	{
		Shown -= OnShownInitialLoad;
		Load();
	}

	private void Build()
	{
		Panel top = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.BG, Padding = new Padding(0, 0, 0, 1) };
		_pathBox = new TextBox { PlaceholderText = "Path to Players\\<GUID> folder", BackColor = Theme.InputBg, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Font = Font };
		Button browseBtn = Btn("\u2026", 28, Theme.Accent, Theme.HdrBg, false);
		browseBtn.Click += delegate { Browse(); };
		_loadBtn = Btn("Load", 92, Theme.TabSelectedText, Theme.Accent, true);
		_loadBtn.Click += delegate { Load(); };
		_saveBtn = Btn("Save", 92, Theme.TabSelectedText, Theme.Accent, true);
		_saveBtn.Enabled = false;
		_saveBtn.Click += delegate { Save(); };
		_creditsBtn = Btn("?", 28, Theme.Accent, Theme.HdrBg, false);
		_creditsBtn.Click += delegate { ShowCredits(); };
		_tip.SetToolTip(_creditsBtn, "Credits");
		top.Controls.AddRange(new Control[] { _pathBox, browseBtn, _loadBtn, _saveBtn, _creditsBtn });
		top.Layout += delegate
		{
			int y = (top.ClientSize.Height - 28) / 2;
			int x = top.ClientSize.Width - 10;
			_creditsBtn.Location = new Point(x - _creditsBtn.Width, y);
			_saveBtn.Location = new Point(_creditsBtn.Left - _saveBtn.Width - 4, y);
			_loadBtn.Location = new Point(_saveBtn.Left - _loadBtn.Width - 4, y);
			browseBtn.Location = new Point(_loadBtn.Left - browseBtn.Width - 6, y);
			_pathBox.Location = new Point(12, y + 1);
			_pathBox.Width = browseBtn.Left - 16;
			_pathBox.Height = 26;
		};

		Panel bottom = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = Theme.BG };
		_creditsLbl = new Label { Dock = DockStyle.Right, Width = 300, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.Dim, Font = Theme.UiFont(7.5f), Padding = new Padding(0, 0, 10, 0), Text = Credits.AttributionLine };
		_statusLbl = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Dim, Font = Theme.UiFont(8f), Padding = new Padding(8, 0, 0, 0) };
		bottom.Controls.Add(_creditsLbl);
		bottom.Controls.Add(_statusLbl);

		Panel center = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.BG2 };
		_actionBarSec = new InvSection("", false);
		_backpackSec = new InvSection("", false);
		_equipSec = new InvSection("", false);
		_accessorySec = new InvSection("", false);
		_ammoSec = new InvSection("", false);

		_profileRow = new Panel { Height = 44, Visible = false, BackColor = Theme.ProfileBand };
		_profileCaptionLbl = new Label { Text = "Profile", AutoSize = true, BackColor = Theme.ProfileBand, ForeColor = Theme.Dim, Font = Theme.UiFont(9.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
		_profileNameLbl = new Label { AutoSize = false, Height = 26, Visible = true, Text = "", AutoEllipsis = true, BackColor = Theme.ProfileBand, ForeColor = Theme.Accent, Font = Theme.TitleFont(11f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
		_profileRow.Controls.Add(_profileCaptionLbl);
		_profileRow.Controls.Add(_profileNameLbl);
		_profileRow.Layout += delegate
		{
			int cy = (_profileRow.Height - _profileCaptionLbl.Height) / 2;
			int x = 14;
			_profileCaptionLbl.Location = new Point(x, cy);
			x = _profileCaptionLbl.Right + 12;
			int nh = Math.Min(26, _profileRow.Height - 8);
			_profileNameLbl.Location = new Point(x, (_profileRow.Height - nh) / 2);
			_profileNameLbl.Width = Math.Max(80, _profileRow.Width - x - 14);
			_profileNameLbl.Height = nh;
		};

		_invTabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.TitleFont(9f, FontStyle.Bold), BackColor = Theme.BG, ForeColor = Theme.HdrText, ItemSize = new Size(132, 30), SizeMode = TabSizeMode.Fixed, DrawMode = TabDrawMode.OwnerDrawFixed, Padding = new Point(0, 0) };
		_invTabs.DrawItem += InvTabs_DrawItem;
		_invTabs.SelectedIndexChanged += delegate { _invTabs.Invalidate(); };
		_invTabs.TabPages.Add(MakeInventoryTab(_actionBarSec));
		_invTabs.TabPages.Add(MakeInventoryTab(_backpackSec));
		_invTabs.TabPages.Add(MakeInventoryTab(_equipSec));
		_invTabs.TabPages.Add(MakeInventoryTab(_accessorySec));
		_invTabs.TabPages.Add(MakeInventoryTab(_ammoSec));

		_workPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BG2, Padding = new Padding(10) };
		_workPanel.Controls.Add(_invTabs);
		_workPanel.Controls.Add(_profileRow);
		_profileRow.Dock = DockStyle.Top;
		_invTabs.Dock = DockStyle.Fill;

		center.Controls.Add(_workPanel);
		Controls.Add(center);
		Controls.Add(top);
		Controls.Add(bottom);
		UpdateUiLanguage();
	}

	private static TabPage MakeInventoryTab(InvSection sec)
	{
		TabPage tab = new TabPage(" ")
		{
			BackColor = Theme.BG2,
			UseVisualStyleBackColor = false,
			AutoScroll = true,
			Padding = new Padding(8)
		};
		sec.Location = new Point(10, 10);
		tab.Controls.Add(sec);
		return tab;
	}

	private void InvTabs_DrawItem(object sender, DrawItemEventArgs e)
	{
		TabControl tc = (TabControl)sender;
		if (e.Index < 0 || e.Index >= tc.TabPages.Count) return;
		TabPage page = tc.TabPages[e.Index];
		Rectangle bounds = e.Bounds;
		bool selected = tc.SelectedIndex == e.Index;
		Color back = selected ? Theme.Accent : Theme.HdrBg;
		Color fore = selected ? Theme.TabSelectedText : Theme.HdrText;
		using SolidBrush brush = new SolidBrush(back);
		e.Graphics.FillRectangle(brush, bounds);
		TextRenderer.DrawText(e.Graphics, page.Text, tc.Font, bounds, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
	}

	private void UpdateProfileHeader()
	{
		if (_save == null)
		{
			_profileRow.Visible = false;
			_profileNameLbl.Text = "";
		}
		else
		{
			string n = (_save.PlayerName ?? "").Trim();
			if (string.IsNullOrEmpty(n)) n = (_save.PlayerGuid ?? "").Trim();
			if (string.IsNullOrEmpty(n)) n = "\u2014";
			_profileNameLbl.Text = n;
			_profileRow.Visible = true;
		}
		_profileRow.PerformLayout();
		_workPanel?.PerformLayout();
		_invTabs?.Invalidate();
	}

	private static string[] ItemDbSearchRoots()
	{
		string bd = AppDomain.CurrentDomain.BaseDirectory;
		string decompiledRoot = Path.GetFullPath(Path.Combine(bd, "..", "..", ".."));
		string bundleOrRepo = Path.GetFullPath(Path.Combine(bd, "..", "..", "..", ".."));
		return new[] { bd, Path.GetFullPath(Path.Combine(bd, "..")), decompiledRoot, bundleOrRepo };
	}

	private bool TryLoadItemDatabaseFromKnownFolders()
	{
		string[] roots = ItemDbSearchRoots();
		foreach (string dir in roots)
		{
			string sqlite = Path.Combine(dir, "windrose_items.db");
			if (File.Exists(sqlite)) { ApplySqliteDb(sqlite); return true; }
			string wrapperDb = Path.Combine(dir, "Wrapper.windrose_items.db");
			if (File.Exists(wrapperDb)) { ApplySqliteDb(wrapperDb); return true; }
		}
		foreach (string dir in roots)
		{
			string json = Path.Combine(dir, "item_db.json");
			if (File.Exists(json)) { ApplyJsonDb(json, dir); return true; }
		}
		foreach (string dir in roots)
		{
			string html = Path.Combine(dir, "Item ID Database.html");
			if (!File.Exists(html)) continue;
			string err = ItemDatabase.Load(html);
			IconCache.Load(dir);
			string value = IconCache.MappedCount > 0 ? $" \u00B7 {IconCache.MappedCount} icons" : "";
			SetStatus(string.IsNullOrEmpty(err) ? $"Item database (HTML): {ItemDatabase.Items.Count} items{value}" : err, !string.IsNullOrEmpty(err));
			return ItemDatabase.Loaded;
		}
		return false;
	}

	private void TryAutoLoadDb()
	{
		if (TryLoadItemDatabaseFromKnownFolders()) return;
		if (BundledItemDatabase.TryGetExtractedPath(out string bundled)) { ApplySqliteDb(bundled); return; }
		string expected = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "windrose_items.db"));
		SetStatus("Item database not found. Place windrose_items.db or Wrapper.windrose_items.db next to the app (expected: " + expected + ")", true);
	}

	private void ApplySqliteDb(string dbPath)
	{
		string e1 = ItemDatabase.LoadFromSqlite(dbPath);
		string e2 = CurveDb.LoadFromSqlite(dbPath);
		IconCache.LoadFromSqlite(dbPath);
		string icons = IconCache.MappedCount > 0 ? $" \u00B7 {IconCache.MappedCount} icons" : " \u00B7 no icons";
		string curves = CurveDb.Loaded ? $" \u00B7 {CurveDb.TableCount} CT ({CurveDb.RowCount} rows)" : "";
		string err = !string.IsNullOrEmpty(e1) ? e1 : e2;
		SetStatus(string.IsNullOrEmpty(err) ? $"Database: {ItemDatabase.Items.Count} items{icons}{curves}" : err, !string.IsNullOrEmpty(err));
	}

	private void ApplyJsonDb(string dbPath, string baseDir)
	{
		string err = ItemDatabase.LoadFromDb(dbPath);
		CurveDb.Load(Path.Combine(baseDir, "curves_db.json"));
		IconCache.Load(baseDir);
		string icons = IconCache.MappedCount > 0 ? $" \u00B7 {IconCache.MappedCount} icons" : " \u00B7 no icons (place windrose_items.db next to the app)";
		string curves = CurveDb.Loaded ? $" \u00B7 {CurveDb.TableCount} CT" : "";
		SetStatus(string.IsNullOrEmpty(err) ? $"Database (JSON): {ItemDatabase.Items.Count} items{icons}{curves}" : err, !string.IsNullOrEmpty(err));
	}

	private void Browse()
	{
		using FolderBrowserDialog dlg = new FolderBrowserDialog { Description = "Select the RocksDB folder for your character (contains CURRENT, *.log or *.wal)", UseDescriptionForTitle = true };
		if (_pathBox.Text.Length > 3) dlg.InitialDirectory = _pathBox.Text;
		if (dlg.ShowDialog() == DialogResult.OK) _pathBox.Text = dlg.SelectedPath;
	}

	private new void Load()
	{
		string p = _pathBox.Text.Trim();
		if (string.IsNullOrEmpty(p)) { SetStatus("Enter the save folder path.", true); return; }
		SetStatus("Loading\u2026");
		Cursor = Cursors.WaitCursor;
		try
		{
			var (saveFile, msg) = SaveFile.Load(p);
			if (saveFile == null) { SetStatus(msg, true); return; }
			_save = saveFile;
			SetStatus("Loaded  \u00B7  " + _save.PlayerGuid);
			DetectRoles();
			Rebuild();
			_saveBtn.Enabled = true;
		}
		finally { Cursor = Cursors.Default; }
	}

	private void Save()
	{
		if (_save == null) return;
		if (!_save.IsModified) { SetStatus("No changes."); return; }
		if (MessageBox.Show("The game must be closed!\n\nCreate a backup and save changes?", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
		string backup = _save.CreateBackup();
		var (ok, err) = _save.Save();
		SetStatus(ok ? ("Saved. Backup: " + backup) : ("Error: " + err), !ok);
		if (!ok) MessageBox.Show(err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
	}

	private static bool IsPoorBackpackModule(ModuleInfo m)
	{
		string? L = m.Label;
		if (string.IsNullOrEmpty(L))
		{
			return false;
		}
		return L.IndexOf("Ship Cosmetics", StringComparison.OrdinalIgnoreCase) >= 0
			|| L.IndexOf(": Crew", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private bool ModuleHasAnyBackpackItemPath(int moduleIndex)
	{
		foreach (InventorySlot s in _save!.GetSlots(moduleIndex))
		{
			if (!s.IsEmpty && s.ItemParams.IndexOf("/backpack/", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private void DetectRoles()
	{
		if (_save == null) return;
		List<ModuleInfo> modules = _save.GetModules();
		_modBackpack = _modActionBar = _modArmor = _modAccessory = _modAmmo = -1;
		int bpLabelIdx = -1;
		int bpLabelCap = -1;
		foreach (ModuleInfo m in modules)
		{
			string? L = m.Label;
			if (string.IsNullOrEmpty(L)) continue;
			if (L.Contains(": Backpack", StringComparison.OrdinalIgnoreCase))
			{
				if (bpLabelIdx < 0 || m.Capacity > bpLabelCap)
				{
					bpLabelCap = m.Capacity;
					bpLabelIdx = m.Index;
				}
				continue;
			}
			if (L.Contains(": Action Bar", StringComparison.OrdinalIgnoreCase) && _modActionBar < 0) { _modActionBar = m.Index; continue; }
			if (L.Contains(": Ammo", StringComparison.OrdinalIgnoreCase) && _modAmmo < 0) { _modAmmo = m.Index; continue; }
			if (L.Contains(": Armor", StringComparison.OrdinalIgnoreCase) && _modArmor < 0) { _modArmor = m.Index; continue; }
			if (L.Contains(": Accessories", StringComparison.OrdinalIgnoreCase) && _modAccessory < 0) { _modAccessory = m.Index; continue; }
		}
		if (bpLabelIdx >= 0)
		{
			_modBackpack = bpLabelIdx;
		}
		Dictionary<int, string> sample = modules.ToDictionary(m => m.Index, m => _save.GetSlots(m.Index).FirstOrDefault(s => !s.IsEmpty)?.ItemParams.ToLowerInvariant() ?? "");
		foreach (ModuleInfo m in modules)
		{
			string s = sample[m.Index];
			if (_modAmmo < 0 && m.Capacity <= 20 && s.Contains("/ammo/")) _modAmmo = m.Index;
			else if (_modArmor < 0 && m.Capacity <= 20 && s.Contains("/armor/")) _modArmor = m.Index;
			else if (_modAccessory < 0 && m.Capacity <= 20 && (s.Contains("/ring/") || s.Contains("/necklace/"))) _modAccessory = m.Index;
			else if (_modActionBar < 0 && m.Capacity == 8 && (s.Contains("/weapon/") || s.Contains("/alchemy/") || s.Contains("/food/") || s.Contains("/consumable/") || s == "")) _modActionBar = m.Index;
		}
		HashSet<int> taken = new HashSet<int> { _modAmmo, _modArmor, _modAccessory, _modActionBar };
		if (_modBackpack < 0)
		{
			IEnumerable<ModuleInfo> pool = modules.Where(mi => !taken.Contains(mi.Index)).Where(mi => !IsPoorBackpackModule(mi));
			ModuleInfo? withBpTag = pool.Where(mi => ModuleHasAnyBackpackItemPath(mi.Index)).OrderByDescending(m => m.Capacity).FirstOrDefault();
			if (withBpTag != null)
			{
				_modBackpack = withBpTag.Index;
			}
			else
			{
				ModuleInfo? pickCap = pool.OrderByDescending(m => m.Capacity).FirstOrDefault();
				if (pickCap != null)
				{
					_modBackpack = pickCap.Index;
				}
			}
		}
		if (_modBackpack < 0 && modules.Count > 0) _modBackpack = modules[0].Index;
		if (_modActionBar < 0 && modules.Count > 3) _modActionBar = modules[3].Index;
		if (_modArmor < 0 && modules.Count > 2) _modArmor = modules[2].Index;
		if (_modAccessory < 0 && modules.Count > 4) _modAccessory = modules[4].Index;
		if (_modAmmo < 0 && modules.Count > 1) _modAmmo = modules[1].Index;
	}

	private void Rebuild()
	{
		if (_save != null)
		{
			Fill(_actionBarSec, _modActionBar, 8);
			Fill(_backpackSec, _modBackpack, 8, 0, false, null, false);
			Fill(_equipSec, _modArmor, 2, SlotConstraints.ArmorSlotCount, false, SlotConstraints.GetArmorName);
			Fill(_accessorySec, _modAccessory, 2, SlotConstraints.AccessorySlotCount, false, SlotConstraints.GetAccessoryName);
			Fill(_ammoSec, _modAmmo, 2, SlotConstraints.AmmoSlotCount, false, SlotConstraints.GetAmmoName);
			UpdateProfileHeader();
			_workPanel?.PerformLayout();
			_invTabs?.Invalidate();
			foreach (TabPage p in _invTabs.TabPages) p.PerformLayout();
		}
	}

	private void Fill(InvSection sec, int mod, int cols, int fixedCount = 0, bool readOnly = false, Func<int, string?>? getSlotName = null, bool showEmptyAddHint = true)
	{
		InventorySlot[] slots;
		if (mod < 0 || _save == null)
		{
			slots = Enumerable.Range(0, Math.Max(0, fixedCount)).Select(i => new InventorySlot { ModuleIndex = Math.Max(mod, 0), SlotIndex = i }).ToArray();
		}
		else if (fixedCount > 0)
		{
			Dictionary<int, InventorySlot> map = new Dictionary<int, InventorySlot>();
			foreach (InventorySlot s in _save.GetSlots(mod)) if (s.SlotIndex >= 0 && s.SlotIndex < fixedCount) map[s.SlotIndex] = s;
			slots = new InventorySlot[fixedCount];
			for (int i = 0; i < fixedCount; i++) slots[i] = map.TryGetValue(i, out InventorySlot v) ? v : new InventorySlot { ModuleIndex = mod, SlotIndex = i };
		}
		else slots = _save.GetSlots(mod).ToArray();

		sec.SetSlots(slots, cols, _tip, readOnly ? null : (Action<InventorySlot>)AddItem, RemoveItem, readOnly ? null : (Action<InventorySlot>)EditItem, getSlotName, showEmptyAddHint);
	}

	private void AddItem(InventorySlot slot)
	{
		if (_save == null) return;
		if (!ItemDatabase.Loaded)
		{
			using OpenFileDialog ofd = new OpenFileDialog { Title = "Select 'Item ID Database.html'", Filter = "HTML|*.html|All|*.*" };
			if (ofd.ShowDialog() != DialogResult.OK) return;
			string err = ItemDatabase.Load(ofd.FileName);
			if (!string.IsNullOrEmpty(err)) { SetStatus(err, true); return; }
		}
		using AddItemDialog dlg = new AddItemDialog(slot.ModuleIndex, slot.SlotIndex);
		if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedItem == null) return;
		if (!_save.AddItem(slot.ModuleIndex, slot.SlotIndex, dlg.SelectedItem.ItemParamsPath, dlg.SelectedLevel, dlg.SelectedCount, dlg.SelectedQuality)) { SetStatus("Could not add item.", true); return; }
		string q = dlg.SelectedQuality > 0 ? $"  Q{dlg.SelectedQuality}" : "";
		SetStatus($"Added: {dlg.SelectedItem.DisplayName}  Lv.{dlg.SelectedLevel}{q}  \u00D7{dlg.SelectedCount}");
		DetectRoles();
		Rebuild();
	}

	private void RemoveItem(InventorySlot slot)
	{
		if (_save == null) return;
		if (MessageBox.Show($"Remove '{slot.InternalName}' from slot {slot.SlotIndex}?", "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
		_save.RemoveItem(slot.ModuleIndex, slot.SlotIndex);
		SetStatus($"Removed: slot {slot.SlotIndex}");
		DetectRoles();
		Rebuild();
	}

	private void EditItem(InventorySlot slot)
	{
		if (_save == null) return;
		ItemEntry? item = ItemDatabase.Items.FirstOrDefault(i => i.ItemParamsPath.Equals(slot.ItemParams, StringComparison.OrdinalIgnoreCase));
		using EditItemDialog dlg = new EditItemDialog(slot, item);
		if (dlg.ShowDialog(this) != DialogResult.OK) return;
		if (!_save.EditItem(slot.ModuleIndex, slot.SlotIndex, dlg.SelectedLevel, dlg.SelectedCount, dlg.SelectedQuality)) { SetStatus("Could not modify item.", true); return; }
		string n = item?.DisplayName ?? slot.InternalName;
		string q = dlg.SelectedQuality > 0 ? $"  Q{dlg.SelectedQuality}" : "";
		SetStatus($"Updated: {n}  Lv.{dlg.SelectedLevel}{q}  \u00D7{dlg.SelectedCount}");
		Rebuild();
	}

	private void ShowCredits()
	{
		using CreditsDialog dlg = new CreditsDialog();
		dlg.ShowDialog(this);
	}

	private void UpdateUiLanguage()
	{
		Text = Credits.WindowTitle;
		_loadBtn.Text = "Load";
		_saveBtn.Text = "Save";
		_pathBox.PlaceholderText = "Path to Players\\<GUID> folder";
		_profileCaptionLbl.Text = "Profile";
		if (_invTabs.TabPages.Count >= 5)
		{
			_invTabs.TabPages[0].Text = "Action Bar";
			_invTabs.TabPages[1].Text = "Backpack";
			_invTabs.TabPages[2].Text = "Equipment";
			_invTabs.TabPages[3].Text = "Accessories";
			_invTabs.TabPages[4].Text = "Ammo";
			_invTabs.Invalidate();
		}
	}

	private void SetStatus(string msg, bool warn = false)
	{
		_statusLbl.ForeColor = warn ? Theme.Warn : Theme.Dim;
		_statusLbl.Text = msg;
	}

	private static Button Btn(string t, int w, Color fg, Color bg, bool primary = false)
	{
		Button b = new Button { Text = t, Width = w, Height = 28, ForeColor = fg, BackColor = bg, FlatStyle = FlatStyle.Flat, Font = Theme.UiFont(8.75f, primary ? FontStyle.Bold : FontStyle.Regular), Cursor = Cursors.Hand };
		b.FlatAppearance.BorderSize = primary ? 0 : 1;
		b.FlatAppearance.BorderColor = Theme.Accent;
		return b;
	}
}
