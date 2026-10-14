using System.Drawing;
using System.Windows.Forms;

namespace CINCROSS;

public class CassetteListForm : Form
{
	private readonly ListBox _lstCassettes;
	private readonly Button _btnChoose;
	private readonly Button _btnCancel;
	private readonly CassetteData[] _cassettes;

	public int SelectedIndex { get; private set; } = -1;

	public CassetteListForm(CassetteData[] cassettes, int currentIndex)
	{
		_cassettes = cassettes;

		Text = "";
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.Manual;
		Size = new Size(260, 400);
		BackColor = Color.FromArgb(35, 35, 35);
		ShowInTaskbar = false;
		TopMost = true;

		Label header = new Label
		{
			Text = "CASSETTE LIST",
			Dock = DockStyle.Top,
			Height = 40,
			BackColor = Color.FromArgb(50, 50, 50),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleCenter
		};

		_lstCassettes = new ListBox
		{
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(40, 40, 40),
			ForeColor = Color.White,
			BorderStyle = BorderStyle.None,
			Font = new Font("Segoe UI", 9f),
			ItemHeight = 15
		};

		for (int i = 0; i < _cassettes.Length; i++)
			_lstCassettes.Items.Add($"{i + 1} - {_cassettes[i].Titulo}");

		if (currentIndex >= 0 && currentIndex < _cassettes.Length)
			_lstCassettes.SelectedIndex = currentIndex;

		Panel bottomPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 34,
			BackColor = Color.FromArgb(50, 50, 50)
		};

		_btnChoose = new Button
		{
			Text = "OK",
			Location = new Point(40, 6),
			Size = new Size(60, 22),
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(60, 60, 60),
			Font = new Font("Segoe UI", 5.5f, FontStyle.Bold)
		};
		_btnChoose.Click += (_, _) => { SelectedIndex = _lstCassettes.SelectedIndex; DialogResult = DialogResult.OK; Close(); };

		_btnCancel = new Button
		{
			Text = "CANCEL",
			Location = new Point(160, 6),
			Size = new Size(60, 22),
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(60, 60, 60),
			Font = new Font("Segoe UI", 5.5f, FontStyle.Bold)
		};
		_btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

		bottomPanel.Controls.Add(_btnCancel);
		bottomPanel.Controls.Add(_btnChoose);

		Controls.Add(_lstCassettes);
		Controls.Add(bottomPanel);
		Controls.Add(header);

		KeyPreview = true;
		KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
	}
}
