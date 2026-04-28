using System.Drawing;
using System.Windows.Forms;

namespace WIM;

public sealed class CreditsDialog : Form
{
	public CreditsDialog()
	{
		Text = "Credits";
		base.ClientSize = new Size(420, 320);
		base.MinimumSize = new Size(360, 260);
		base.BackColor = Theme.BG2;
		base.ForeColor = Theme.Text;
		base.Font = Theme.UiFont(9f);
		base.StartPosition = FormStartPosition.CenterParent;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.ShowInTaskbar = false;
		base.KeyPreview = true;
		base.KeyDown += delegate(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				base.DialogResult = DialogResult.Cancel;
				Close();
			}
		};
		Label title = new Label
		{
			Text = Credits.AppDisplayName,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleCenter,
			ForeColor = Theme.Accent,
			Font = Theme.TitleFont(10f, FontStyle.Bold),
			Padding = new Padding(12, 10, 12, 6)
		};
		TextBox body = new TextBox
		{
			Multiline = true,
			ReadOnly = true,
			ScrollBars = ScrollBars.Vertical,
			BorderStyle = BorderStyle.None,
			BackColor = Theme.BG2,
			ForeColor = Theme.Text,
			Font = Theme.UiFont(9f),
			Text = Credits.BuildCreditsBody(),
			Dock = DockStyle.Fill,
			TabStop = false,
			Margin = new Padding(16, 0, 16, 0)
		};
		Panel bottom = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Theme.BG2,
			Height = 44
		};
		Button okBtn = new Button
		{
			Text = "OK",
			Width = 88,
			Height = 28,
			FlatStyle = FlatStyle.Flat,
			BackColor = Theme.Accent,
			ForeColor = Theme.TabSelectedText,
			DialogResult = DialogResult.OK,
			Font = Theme.UiFont(8.75f, FontStyle.Bold),
			Cursor = Cursors.Hand
		};
		okBtn.FlatAppearance.BorderSize = 0;
		bottom.Controls.Add(okBtn);
		bottom.Layout += delegate
		{
			okBtn.Location = new Point((bottom.Width - okBtn.Width) / 2, (bottom.Height - okBtn.Height) / 2);
		};
		base.AcceptButton = okBtn;
		TableLayoutPanel grid = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			BackColor = Theme.BG2,
			Padding = new Padding(0, 0, 0, 8)
		};
		grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
		grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
		grid.Controls.Add(title, 0, 0);
		grid.Controls.Add(body, 0, 1);
		grid.Controls.Add(bottom, 0, 2);
		base.Controls.Add(grid);
	}
}
