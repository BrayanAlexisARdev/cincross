using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CINCROSS;

public class EditTaskForm : Form
{
	private readonly List<string> _availableIcons = new();
	private int _currentIconIndex;
	private string _imgDir = "";

	private readonly TextBox txtTaskName;
	private readonly PictureBox picIconPreview;
	private readonly Button btnPrevIcon;
	private readonly Button btnNextIcon;
	private readonly ComboBox cmbHours;
	private readonly ComboBox cmbMinutes;
	private readonly Button btnOk;
	private readonly Button btnCancel;

	public string TaskName { get; private set; } = "";
	public TimeSpan TaskTime { get; private set; }
	public string SelectedIcon { get; private set; } = "tasks_TSK.png";

	public EditTaskForm(string title, string currentName, TimeSpan currentTime, string currentIcon)
	{
		Text = "";
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.Manual;
		Size = new Size(280, 310);
		BackColor = Color.FromArgb(35, 35, 35);
		ShowInTaskbar = false;
		TopMost = true;

		Label header = new Label
		{
			Text = title,
			Dock = DockStyle.Top,
			Height = 40,
			BackColor = Color.FromArgb(50, 50, 50),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleCenter
		};

		int y = 55;
		int labelW = 60;
		int fieldX = 70;
		int fieldW = 190;

		Label lblName = new Label
		{
			Text = "NAME",
			Location = new Point(15, y + 4),
			Size = new Size(labelW, 18),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
		};
		txtTaskName = new TextBox
		{
			Text = currentName,
			Location = new Point(fieldX, y - 1),
			Size = new Size(fieldW / 2, 20),
			MaxLength = 8,
			BackColor = Color.FromArgb(30, 30, 30),
			ForeColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 8f)
		};
		Label lblLimit = new Label
		{
			Text = "limit 8",
			Location = new Point(fieldX + fieldW / 2 + 4, y + 2),
			Size = new Size(80, 16),
			ForeColor = Color.Red,
			Font = new Font("Segoe UI", 7f)
		};
		y += 32;

		Label lblIcon = new Label
		{
			Text = "ICON",
			Location = new Point(15, y + 4),
			Size = new Size(labelW, 18),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
		};
		picIconPreview = new PictureBox
		{
			Location = new Point(fieldX, y),
			Size = new Size(48, 48),
			SizeMode = PictureBoxSizeMode.Zoom,
			BackColor = Color.FromArgb(30, 30, 30),
			BorderStyle = BorderStyle.FixedSingle
		};
		btnPrevIcon = new Button
		{
			Text = "<",
			Location = new Point(fieldX + 52, y + 12),
			Size = new Size(30, 22),
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(50, 50, 50),
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};
		btnNextIcon = new Button
		{
			Text = ">",
			Location = new Point(fieldX + 86, y + 12),
			Size = new Size(30, 22),
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(50, 50, 50),
			Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
		};
		btnPrevIcon.Click += (_, _) => CycleIcon(-1);
		btnNextIcon.Click += (_, _) => CycleIcon(1);
		y += 54;

		Label lblTime = new Label
		{
			Text = "TIME",
			Location = new Point(15, y + 4),
			Size = new Size(labelW, 18),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
		};
		cmbHours = new ComboBox
		{
			Location = new Point(fieldX, y),
			Size = new Size(52, 20),
			DropDownStyle = ComboBoxStyle.DropDownList,
			BackColor = Color.FromArgb(30, 30, 30),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 8f)
		};
		for (int i = 0; i <= 3; i++) cmbHours.Items.Add(i.ToString());

		Label lblHs = new Label
		{
			Text = "hs",
			Location = new Point(fieldX + 55, y + 4),
			Size = new Size(20, 16),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 7f)
		};

		cmbMinutes = new ComboBox
		{
			Location = new Point(fieldX + 78, y),
			Size = new Size(52, 20),
			DropDownStyle = ComboBoxStyle.DropDownList,
			BackColor = Color.FromArgb(30, 30, 30),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 8f)
		};
		foreach (int m in new[] { 0, 15, 30, 45 }) cmbMinutes.Items.Add(m.ToString());

		Label lblMin = new Label
		{
			Text = "min",
			Location = new Point(fieldX + 133, y + 4),
			Size = new Size(25, 16),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 7f)
		};
		y += 45;

		Panel bottomPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 34,
			BackColor = Color.FromArgb(50, 50, 50)
		};

		btnOk = new Button
		{
			Text = "OK",
			Location = new Point(50, 6),
			Size = new Size(60, 22),
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(60, 60, 60),
			Font = new Font("Segoe UI", 5.5f, FontStyle.Bold)
		};
		btnOk.Click += BtnOk_Click;

		btnCancel = new Button
		{
			Text = "CANCEL",
			Location = new Point(170, 6),
			Size = new Size(60, 22),
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(60, 60, 60),
			Font = new Font("Segoe UI", 5.5f, FontStyle.Bold)
		};
		btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

		bottomPanel.Controls.Add(btnCancel);
		bottomPanel.Controls.Add(btnOk);

		Controls.Add(bottomPanel);
		Controls.Add(lblMin);
		Controls.Add(cmbMinutes);
		Controls.Add(lblHs);
		Controls.Add(cmbHours);
		Controls.Add(lblTime);
		Controls.Add(btnNextIcon);
		Controls.Add(btnPrevIcon);
		Controls.Add(picIconPreview);
		Controls.Add(lblIcon);
		Controls.Add(txtTaskName);
		Controls.Add(lblLimit);
		Controls.Add(lblName);
		Controls.Add(header);

		LoadIcons();
		SetCurrentIcon(currentIcon);
		cmbHours.SelectedIndex = Math.Min(currentTime.Hours, 3);
		int[] minuteOptions = new[] { 0, 15, 30, 45 };
		int closest = 0;
		for (int i = 0; i < minuteOptions.Length; i++)
		{
			if (Math.Abs(minuteOptions[i] - currentTime.Minutes) < Math.Abs(minuteOptions[closest] - currentTime.Minutes))
				closest = i;
		}
		cmbMinutes.SelectedIndex = closest;

		KeyPreview = true;
		KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
	}

	private void LoadIcons()
	{
		_availableIcons.Clear();
		_imgDir = PathHelper.GetImgDir();
		if (!string.IsNullOrEmpty(_imgDir) && Directory.Exists(_imgDir))
		{
			foreach (string f in Directory.GetFiles(_imgDir, "*_TSK.png"))
				_availableIcons.Add(Path.GetFileName(f));
		}
		if (_availableIcons.Count == 0)
			_availableIcons.Add("tasks_TSK.png");
	}

	private void SetCurrentIcon(string iconName)
	{
		int idx = _availableIcons.IndexOf(iconName);
		_currentIconIndex = idx >= 0 ? idx : 0;
		UpdateIconPreview();
	}

	private void CycleIcon(int dir)
	{
		if (_availableIcons.Count == 0) return;
		_currentIconIndex = (_currentIconIndex + dir + _availableIcons.Count) % _availableIcons.Count;
		UpdateIconPreview();
	}

	private void UpdateIconPreview()
	{
		if (_currentIconIndex < 0 || _currentIconIndex >= _availableIcons.Count) return;
		string iconFile = Path.Combine(_imgDir, _availableIcons[_currentIconIndex]);
		if (File.Exists(iconFile))
		{
			try
			{
				if (picIconPreview.Image != null) picIconPreview.Image.Dispose();
				picIconPreview.Image = PathHelper.LoadImage(iconFile);
			}
			catch (Exception ex) { Logger.Error("EditTaskForm.UpdateIconPreview", ex); }
		}
	}

	private void BtnOk_Click(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(txtTaskName.Text))
		{
			MessageBox.Show("Task name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		int h = int.Parse(cmbHours.SelectedItem?.ToString() ?? "0");
		int m = int.Parse(cmbMinutes.SelectedItem?.ToString() ?? "0");
		if (h == 0 && m == 0)
		{
			MessageBox.Show("Time must be greater than 0.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		TaskName = txtTaskName.Text;
		TaskTime = new TimeSpan(h, m, 0);
		SelectedIcon = _currentIconIndex >= 0 && _currentIconIndex < _availableIcons.Count ? _availableIcons[_currentIconIndex] : "tasks_TSK.png";
		DialogResult = DialogResult.OK;
		Close();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (picIconPreview.Image != null)
			{
				picIconPreview.Image.Dispose();
				picIconPreview.Image = null;
			}
		}
		base.Dispose(disposing);
	}
}
