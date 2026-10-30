using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PdfiumViewer;

namespace CINCROSS;

public class FormPdfViewer : Form
{
	static FormPdfViewer()
	{
		PdfiumResolver.Resolve += (_, e) => e.PdfiumFileName = FindPdfium();
	}

	private static string? FindPdfium()
	{
		string baseDir = AppContext.BaseDirectory;
		string[] candidates = new string[]
		{
			Path.Combine(baseDir, "x64", "pdfium.dll"),
			Path.Combine(baseDir, "pdfium.dll"),
			Path.Combine(Path.GetDirectoryName(typeof(FormPdfViewer).Assembly.Location) ?? string.Empty, "x64", "pdfium.dll")
		};

		foreach (string candidate in candidates)
		{
			if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}

	public FormPdfViewer(string pdfPath, Form owner)
	{
		Text = "";
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.Manual;
		Size = new Size(600, 800);
		Location = new Point(owner.Left - 600, owner.Top);
		BackColor = Color.FromArgb(35, 35, 35);
		ShowInTaskbar = false;
		TopMost = true;

		Panel header = new Panel
		{
			BackColor = Color.FromArgb(50, 50, 50),
			Dock = DockStyle.Top,
			Height = 36
		};

		Label lblTitle = new Label
		{
			Text = "MANUAL PDF",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Location = new Point(12, 0),
			Size = new Size(200, 36)
		};

		Button btnClose = new Button
		{
			Text = "X",
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0 },
			ForeColor = Color.White,
			BackColor = Color.FromArgb(60, 60, 60),
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			Size = new Size(36, 26),
			Location = new Point(600 - 42, 5),
			Cursor = Cursors.Hand
		};
		btnClose.Click += (_, _) => Close();

		header.Controls.Add(lblTitle);
		header.Controls.Add(btnClose);

		PdfViewer pdfViewer = new PdfViewer
		{
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(35, 35, 35),
			ShowToolbar = true,
			ShowBookmarks = true
		};

		if (System.IO.File.Exists(pdfPath))
		{
			pdfViewer.Document = PdfDocument.Load(pdfPath);
		}

		Controls.Add(pdfViewer);
		Controls.Add(header);

		KeyPreview = true;
		KeyDown += (_, e) =>
		{
			if (e.KeyCode == Keys.Escape) Close();
		};

		FormClosed += (_, _) =>
		{
			pdfViewer.Document?.Dispose();
		};
	}
}
