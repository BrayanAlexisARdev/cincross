using System.Drawing;
using System.Windows.Forms;

namespace CINCROSS;

public class InfoForm : Form
{
	private readonly Button _btnOk;

	public InfoForm()
	{
		Text = "";
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.Manual;
		Size = new Size(360, 380);
		BackColor = Color.FromArgb(35, 35, 35);
		ShowInTaskbar = false;
		TopMost = true;

		Label header = new Label
		{
			Text = "INFORMACION",
			Dock = DockStyle.Top,
			Height = 40,
			BackColor = Color.FromArgb(50, 50, 50),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleCenter
		};

		Label infoText = new Label
		{
			Text = "Esta aplicacion es un reproductor de radio online con temporizador que facilita el acceso a streams publicos.\n\nCINCROSS no es propietaria, ni tiene relacion comercial con estas emisoras reproducidas. Todos los derechos de los contenidos pertenecen exclusivamente a sus respectivos duenos.\n\nSi usted es titular de una emisora y desea que esta sea retirada de nuestra plataforma, por favor contactenos en X@gmail.com.",
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(35, 35, 35),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 8f),
			Padding = new Padding(15, 15, 15, 5),
			TextAlign = ContentAlignment.TopLeft
		};

		Panel bottomPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 34,
			BackColor = Color.FromArgb(50, 50, 50)
		};

		_btnOk = new Button
		{
			Text = "OK",
			Location = new Point(110, 6),
			Size = new Size(100, 22),
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(60, 60, 60),
			Font = new Font("Segoe UI", 7f, FontStyle.Bold)
		};
		_btnOk.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

		bottomPanel.Controls.Add(_btnOk);

		Controls.Add(infoText);
		Controls.Add(bottomPanel);
		Controls.Add(header);

		KeyPreview = true;
		KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
	}
}
