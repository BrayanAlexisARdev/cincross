using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CINCROSS;

public class AddTaskForm : Form
{
	private List<string> _availableIcons = new List<string>();

	private int _currentIconIndex;

	private string _imgDir = "";

	private bool _dragging;

	private Point _startPoint = new Point(0, 0);

	private IContainer components = null!;

	private Label labelTaskName = null!;

	private TextBox txtTaskName = null!;

	private Label labelTime = null!;

	private ComboBox cmbHours = null!;

	private ComboBox cmbMinutes = null!;

	private Label labelHours = null!;

	private Label labelMinutes = null!;

	private Label labelM3u = null!;

	private TextBox txtM3uPath = null!;

	private Button btnBrowseM3u = null!;

	private Button btnAccept = null!;

	private Button btnCancel = null!;

	private Label labelIcon = null!;

	private PictureBox picIconPreview = null!;

	private Button btnPrevIcon = null!;

	private Button btnNextIcon = null!;

	private OpenFileDialog openFileDialogM3u = null!;

	public string TaskName { get; private set; }

	public TimeSpan TaskTime { get; private set; }

	public string M3uPath { get; private set; }

	public string SelectedIcon { get; private set; }

	public AddTaskForm()
	{
		this.components = new System.ComponentModel.Container();
		InitializeComponent();
		BackColor = Color.FromArgb(45, 45, 48);
		ForeColor = Color.White;
		TaskName = "";
		M3uPath = "";
		SelectedIcon = "tasks_TSK.png";
		StyleButton(btnAccept, Color.FromArgb(0, 122, 204));
		StyleButton(btnCancel, Color.FromArgb(63, 63, 70));
		StyleButton(btnBrowseM3u, Color.FromArgb(63, 63, 70));
		StyleButton(btnPrevIcon, Color.FromArgb(63, 63, 70));
		StyleButton(btnNextIcon, Color.FromArgb(63, 63, 70));
		txtTaskName.BackColor = Color.FromArgb(30, 30, 30);
		txtTaskName.ForeColor = Color.White;
		txtTaskName.BorderStyle = BorderStyle.FixedSingle;
		txtM3uPath.BackColor = Color.FromArgb(30, 30, 30);
		txtM3uPath.ForeColor = Color.White;
		txtM3uPath.BorderStyle = BorderStyle.FixedSingle;
		cmbHours.BackColor = Color.FromArgb(30, 30, 30);
		cmbHours.ForeColor = Color.White;
		cmbHours.FlatStyle = FlatStyle.Flat;
		cmbMinutes.BackColor = Color.FromArgb(30, 30, 30);
		cmbMinutes.ForeColor = Color.White;
		cmbMinutes.FlatStyle = FlatStyle.Flat;
		foreach (Control c in base.Controls)
		{
			if (c is Label)
			{
				c.ForeColor = Color.White;
			}
		}
		base.MouseDown += delegate(object? s, MouseEventArgs e)
		{
			_dragging = true;
			_startPoint = new Point(e.X, e.Y);
		};
		base.MouseUp += delegate
		{
			_dragging = false;
		};
		base.MouseMove += delegate(object? s, MouseEventArgs e)
		{
			if (_dragging)
			{
				Point point = PointToScreen(e.Location);
				base.Location = new Point(point.X - _startPoint.X, point.Y - _startPoint.Y);
			}
		};
		LoadIcons();
		btnPrevIcon.Click += delegate
		{
			CycleIcon(-1);
		};
		btnNextIcon.Click += delegate
		{
			CycleIcon(1);
		};
		cmbHours.SelectedIndex = 0;
		cmbMinutes.SelectedIndex = 2;
		FontHelper.ApplyFont(this, 10f);
	}

	private void StyleButton(Button btn, Color backColor)
	{
		btn.FlatStyle = FlatStyle.Flat;
		btn.BackColor = backColor;
		btn.ForeColor = Color.White;
		btn.FlatAppearance.BorderSize = 0;
	}

	private void LoadIcons()
	{
		try
		{
			_imgDir = PathHelper.GetImgDir();
			if (string.IsNullOrEmpty(_imgDir))
			{
				return;
			}
			string[] files = Directory.GetFiles(_imgDir, "*_TSK.png");
			foreach (string file in files)
			{
				_availableIcons.Add(Path.GetFileName(file));
			}
			if (_availableIcons.Count > 0)
			{
				_currentIconIndex = _availableIcons.IndexOf("tasks_TSK.png");
				if (_currentIconIndex == -1)
				{
					_currentIconIndex = 0;
				}
				UpdateIconPreview();
			}
		}
		catch (Exception ex)
		{
			Logger.Error("AddTaskForm.LoadIcons", ex);
		}
	}

	private void CycleIcon(int direction)
	{
		if (_availableIcons.Count != 0)
		{
			_currentIconIndex = (_currentIconIndex + direction + _availableIcons.Count) % _availableIcons.Count;
			UpdateIconPreview();
		}
	}

	private void UpdateIconPreview()
	{
		if (_currentIconIndex < 0 || _currentIconIndex >= _availableIcons.Count)
		{
			return;
		}
		string iconName = _availableIcons[_currentIconIndex];
		string fullPath = Path.Combine(_imgDir, iconName);
		if (File.Exists(fullPath))
		{
			if (picIconPreview.Image != null)
			{
				picIconPreview.Image.Dispose();
			}
			picIconPreview.Image = PathHelper.LoadImage(fullPath);
			SelectedIcon = iconName;
		}
	}

	private void btnBrowseM3u_Click(object? sender, EventArgs e)
	{
		if (openFileDialogM3u.ShowDialog() == DialogResult.OK)
		{
			txtM3uPath.Text = openFileDialogM3u.FileName;
		}
	}

	private void btnAccept_Click(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(txtTaskName.Text))
		{
			MessageBox.Show("El nombre de la tarea no puede estar vacío.", "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		int h = int.Parse(cmbHours.SelectedItem?.ToString() ?? "0");
		int m = int.Parse(cmbMinutes.SelectedItem?.ToString() ?? "0");
		if (h == 0 && m == 0)
		{
			MessageBox.Show("El tiempo debe ser mayor a 0.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		TaskName = txtTaskName.Text;
		TaskTime = new TimeSpan(h, m, 0);
		M3uPath = txtM3uPath.Text;
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private void btnCancel_Click(object? sender, EventArgs e)
	{
		base.DialogResult = DialogResult.Cancel;
		Close();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.labelTaskName = new System.Windows.Forms.Label();
		this.txtTaskName = new System.Windows.Forms.TextBox();
		this.labelTime = new System.Windows.Forms.Label();
		this.cmbHours = new System.Windows.Forms.ComboBox();
		this.cmbMinutes = new System.Windows.Forms.ComboBox();
		this.labelHours = new System.Windows.Forms.Label();
		this.labelMinutes = new System.Windows.Forms.Label();
		this.labelM3u = new System.Windows.Forms.Label();
		this.txtM3uPath = new System.Windows.Forms.TextBox();
		this.btnBrowseM3u = new System.Windows.Forms.Button();
		this.btnAccept = new System.Windows.Forms.Button();
		this.btnCancel = new System.Windows.Forms.Button();
		this.labelIcon = new System.Windows.Forms.Label();
		this.picIconPreview = new System.Windows.Forms.PictureBox();
		this.btnPrevIcon = new System.Windows.Forms.Button();
		this.btnNextIcon = new System.Windows.Forms.Button();
		this.openFileDialogM3u = new System.Windows.Forms.OpenFileDialog();
		((System.ComponentModel.ISupportInitialize)this.picIconPreview).BeginInit();
		base.SuspendLayout();
		this.labelTaskName.AutoSize = true;
		this.labelTaskName.Location = new System.Drawing.Point(12, 15);
		this.labelTaskName.Name = "labelTaskName";
		this.labelTaskName.Size = new System.Drawing.Size(47, 20);
		this.labelTaskName.TabIndex = 0;
		this.labelTaskName.Text = "Tarea:";
		this.txtTaskName.Location = new System.Drawing.Point(148, 12);
		this.txtTaskName.Name = "txtTaskName";
		this.txtTaskName.Size = new System.Drawing.Size(224, 27);
		this.txtTaskName.TabIndex = 1;
		this.labelIcon.AutoSize = true;
		this.labelIcon.Location = new System.Drawing.Point(12, 55);
		this.labelIcon.Name = "labelIcon";
		this.labelIcon.Size = new System.Drawing.Size(49, 20);
		this.labelIcon.TabIndex = 12;
		this.labelIcon.Text = "Icono:";
		this.picIconPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.picIconPreview.Location = new System.Drawing.Point(220, 45);
		this.picIconPreview.Name = "picIconPreview";
		this.picIconPreview.Size = new System.Drawing.Size(60, 60);
		this.picIconPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
		this.picIconPreview.TabIndex = 13;
		this.picIconPreview.TabStop = false;
		this.btnPrevIcon.Location = new System.Drawing.Point(180, 60);
		this.btnPrevIcon.Name = "btnPrevIcon";
		this.btnPrevIcon.Size = new System.Drawing.Size(30, 30);
		this.btnPrevIcon.TabIndex = 14;
		this.btnPrevIcon.Text = "<";
		this.btnPrevIcon.UseVisualStyleBackColor = true;
		this.btnNextIcon.Location = new System.Drawing.Point(290, 60);
		this.btnNextIcon.Name = "btnNextIcon";
		this.btnNextIcon.Size = new System.Drawing.Size(30, 30);
		this.btnNextIcon.TabIndex = 15;
		this.btnNextIcon.Text = ">";
		this.btnNextIcon.UseVisualStyleBackColor = true;
		this.labelTime.AutoSize = true;
		this.labelTime.Location = new System.Drawing.Point(12, 120);
		this.labelTime.Name = "labelTime";
		this.labelTime.Size = new System.Drawing.Size(63, 20);
		this.labelTime.TabIndex = 2;
		this.labelTime.Text = "Tiempo:";
		this.cmbHours.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmbHours.FormattingEnabled = true;
		this.cmbHours.Items.AddRange("0", "1", "2", "3");
		this.cmbHours.Location = new System.Drawing.Point(148, 117);
		this.cmbHours.Name = "cmbHours";
		this.cmbHours.Size = new System.Drawing.Size(60, 28);
		this.cmbHours.TabIndex = 3;
		this.labelHours.AutoSize = true;
		this.labelHours.Location = new System.Drawing.Point(214, 120);
		this.labelHours.Name = "labelHours";
		this.labelHours.Size = new System.Drawing.Size(25, 20);
		this.labelHours.TabIndex = 5;
		this.labelHours.Text = "hs";
		this.cmbMinutes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmbMinutes.FormattingEnabled = true;
		this.cmbMinutes.Items.AddRange("0", "15", "30", "45");
		this.cmbMinutes.Location = new System.Drawing.Point(245, 117);
		this.cmbMinutes.Name = "cmbMinutes";
		this.cmbMinutes.Size = new System.Drawing.Size(60, 28);
		this.cmbMinutes.TabIndex = 4;
		this.labelMinutes.AutoSize = true;
		this.labelMinutes.Location = new System.Drawing.Point(311, 120);
		this.labelMinutes.Name = "labelMinutes";
		this.labelMinutes.Size = new System.Drawing.Size(34, 20);
		this.labelMinutes.TabIndex = 6;
		this.labelMinutes.Text = "min";
		this.labelM3u.AutoSize = true;
		this.labelM3u.Location = new System.Drawing.Point(12, 160);
		this.labelM3u.Name = "labelM3u";
		this.labelM3u.Size = new System.Drawing.Size(126, 20);
		this.labelM3u.TabIndex = 7;
		this.labelM3u.Text = "Audio / Playlist:";
		this.txtM3uPath.Location = new System.Drawing.Point(148, 157);
		this.txtM3uPath.Name = "txtM3uPath";
		this.txtM3uPath.ReadOnly = true;
		this.txtM3uPath.Size = new System.Drawing.Size(140, 27);
		this.txtM3uPath.TabIndex = 8;
		this.btnBrowseM3u.Location = new System.Drawing.Point(294, 156);
		this.btnBrowseM3u.Name = "btnBrowseM3u";
		this.btnBrowseM3u.Size = new System.Drawing.Size(78, 29);
		this.btnBrowseM3u.TabIndex = 9;
		this.btnBrowseM3u.Text = "...";
		this.btnBrowseM3u.UseVisualStyleBackColor = true;
		this.btnBrowseM3u.Click += new System.EventHandler(btnBrowseM3u_Click);
		this.btnAccept.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
		this.btnAccept.Location = new System.Drawing.Point(148, 210);
		this.btnAccept.Name = "btnAccept";
		this.btnAccept.Size = new System.Drawing.Size(100, 40);
		this.btnAccept.TabIndex = 10;
		this.btnAccept.Text = "ACEPTAR";
		this.btnAccept.UseVisualStyleBackColor = true;
		this.btnAccept.Click += new System.EventHandler(btnAccept_Click);
		this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
		this.btnCancel.Location = new System.Drawing.Point(272, 210);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(100, 40);
		this.btnCancel.TabIndex = 11;
		this.btnCancel.Text = "CANCELAR";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
		this.openFileDialogM3u.Filter = "Archivos de Audio|*.m3u;*.mp3;*.wav;*.ogg|Todos los archivos|*.*";
		this.openFileDialogM3u.Title = "Seleccionar Audio o Playlist";
		base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 20f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(384, 270);
		base.Controls.Add(this.btnNextIcon);
		base.Controls.Add(this.btnPrevIcon);
		base.Controls.Add(this.picIconPreview);
		base.Controls.Add(this.labelIcon);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.btnAccept);
		base.Controls.Add(this.btnBrowseM3u);
		base.Controls.Add(this.txtM3uPath);
		base.Controls.Add(this.labelM3u);
		base.Controls.Add(this.labelMinutes);
		base.Controls.Add(this.cmbMinutes);
		base.Controls.Add(this.labelHours);
		base.Controls.Add(this.cmbHours);
		base.Controls.Add(this.labelTime);
		base.Controls.Add(this.txtTaskName);
		base.Controls.Add(this.labelTaskName);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		base.Name = "AddTaskForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Agregar Nueva Tarea";
		((System.ComponentModel.ISupportInitialize)this.picIconPreview).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
