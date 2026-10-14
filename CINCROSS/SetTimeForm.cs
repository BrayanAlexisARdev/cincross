using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CINCROSS;

public class SetTimeForm : Form
{
	private IContainer components = null!;

	private Label label1 = null!;

	private Label label2 = null!;

	private NumericUpDown numericUpDownHours = null!;

	private NumericUpDown numericUpDownMinutes = null!;

	private Button btnSet = null!;

	private Button btnStart = null!;

	public int SelectedHours { get; private set; }

	public int SelectedMinutes { get; private set; }

	public bool StartImmediately { get; private set; }

	public SetTimeForm()
	{
		this.components = new System.ComponentModel.Container();
		InitializeComponent();
		BackColor = Color.FromArgb(45, 45, 48);
		ForeColor = Color.White;
		foreach (Control control in base.Controls)
		{
			control.ForeColor = Color.White;
			if (control is Button b)
			{
				b.FlatStyle = FlatStyle.Flat;
				b.BackColor = Color.FromArgb(63, 63, 70);
				b.FlatAppearance.BorderSize = 0;
			}
			if (control is NumericUpDown n)
			{
				n.BackColor = Color.FromArgb(30, 30, 30);
				n.ForeColor = Color.White;
			}
		}
		FontHelper.ApplyFont(this, 10f);
	}

	private void btnSet_Click(object? sender, EventArgs e)
	{
		SaveValues();
		StartImmediately = false;
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private void btnStart_Click(object? sender, EventArgs e)
	{
		SaveValues();
		StartImmediately = true;
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private void SaveValues()
	{
		SelectedHours = (int)numericUpDownHours.Value;
		SelectedMinutes = (int)numericUpDownMinutes.Value;
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
		this.label1 = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this.numericUpDownHours = new System.Windows.Forms.NumericUpDown();
		this.numericUpDownMinutes = new System.Windows.Forms.NumericUpDown();
		this.btnSet = new System.Windows.Forms.Button();
		this.btnStart = new System.Windows.Forms.Button();
		((System.ComponentModel.ISupportInitialize)this.numericUpDownHours).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.numericUpDownMinutes).BeginInit();
		base.SuspendLayout();
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(12, 9);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(56, 20);
		this.label1.TabIndex = 0;
		this.label1.Text = "Horas:";
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(12, 48);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(65, 20);
		this.label2.TabIndex = 1;
		this.label2.Text = "Minutos:";
		this.numericUpDownHours.Location = new System.Drawing.Point(83, 7);
		this.numericUpDownHours.Maximum = new decimal(new int[4] { 24, 0, 0, 0 });
		this.numericUpDownHours.Name = "numericUpDownHours";
		this.numericUpDownHours.Size = new System.Drawing.Size(75, 27);
		this.numericUpDownHours.TabIndex = 2;
		this.numericUpDownMinutes.Location = new System.Drawing.Point(83, 46);
		this.numericUpDownMinutes.Maximum = new decimal(new int[4] { 60, 0, 0, 0 });
		this.numericUpDownMinutes.Name = "numericUpDownMinutes";
		this.numericUpDownMinutes.Size = new System.Drawing.Size(75, 27);
		this.numericUpDownMinutes.TabIndex = 3;
		this.btnSet.Location = new System.Drawing.Point(12, 85);
		this.btnSet.Name = "btnSet";
		this.btnSet.Size = new System.Drawing.Size(70, 35);
		this.btnSet.TabIndex = 4;
		this.btnSet.Text = "Setear";
		this.btnSet.UseVisualStyleBackColor = true;
		this.btnSet.Click += new System.EventHandler(btnSet_Click);
		this.btnStart.Location = new System.Drawing.Point(88, 85);
		this.btnStart.Name = "btnStart";
		this.btnStart.Size = new System.Drawing.Size(70, 35);
		this.btnStart.TabIndex = 5;
		this.btnStart.Text = "Empezar";
		this.btnStart.UseVisualStyleBackColor = true;
		this.btnStart.Click += new System.EventHandler(btnStart_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 20f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(170, 132);
		base.Controls.Add(this.btnStart);
		base.Controls.Add(this.btnSet);
		base.Controls.Add(this.numericUpDownMinutes);
		base.Controls.Add(this.numericUpDownHours);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.label1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
		base.Name = "SetTimeForm";
		this.Text = "Configuracion general";
		((System.ComponentModel.ISupportInitialize)this.numericUpDownHours).EndInit();
		((System.ComponentModel.ISupportInitialize)this.numericUpDownMinutes).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
