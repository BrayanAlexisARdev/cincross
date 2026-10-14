using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace CINCROSS;

public class FormYoutube : Form
{
	public FormYoutube(Form owner)
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
			Height = 40
		};

		Label lblTitle = new Label
		{
			Text = "YOUTUBE",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Location = new Point(12, 0),
			Size = new Size(100, 40)
		};

		TextBox txtUrl = new TextBox
		{
			Text = "https://www.yout-ube.com/watch?v=v8H5ACDqlHo&t=4s",
			ForeColor = Color.FromArgb(180, 180, 180),
			BackColor = Color.FromArgb(60, 60, 60),
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 8f),
			Location = new Point(110, 9),
			Size = new Size(390, 22),
			TabIndex = 0
		};
		txtUrl.GotFocus += (_, _) =>
		{
			if (txtUrl.Text == "https://www.yout-ube.com/watch?v=v8H5ACDqlHo&t=4s")
			{ txtUrl.Text = ""; txtUrl.ForeColor = Color.White; }
		};
		txtUrl.LostFocus += (_, _) =>
		{
			if (string.IsNullOrWhiteSpace(txtUrl.Text))
			{ txtUrl.Text = "https://www.yout-ube.com/watch?v=v8H5ACDqlHo&t=4s"; txtUrl.ForeColor = Color.FromArgb(180, 180, 180); }
		};

		Button btnGo = new Button
		{
			Text = "▶",
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0 },
			ForeColor = Color.White,
			BackColor = Color.FromArgb(220, 60, 60),
			Font = new Font("Segoe UI", 8f, FontStyle.Bold),
			Size = new Size(30, 22),
			Location = new Point(506, 9),
			Cursor = Cursors.Hand
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
			Location = new Point(600 - 42, 7),
			Cursor = Cursors.Hand
		};
		btnClose.Click += (_, _) => Close();

		header.Controls.Add(lblTitle);
		header.Controls.Add(txtUrl);
		header.Controls.Add(btnGo);
		header.Controls.Add(btnClose);

		WebView2 webView = new WebView2
		{
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(35, 35, 35)
		};
		webView.CoreWebView2InitializationCompleted += (_, _) =>
		{
			webView.CoreWebView2.Settings.IsScriptEnabled = true;
			webView.CoreWebView2.Navigate("https://www.yout-ube.com/watch?v=v8H5ACDqlHo&t=4s");
		};
		_ = webView.EnsureCoreWebView2Async();

		btnGo.Click += (_, _) =>
		{
			string input = txtUrl.Text.Trim();
			if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
			    (uri.Scheme == "http" || uri.Scheme == "https"))
			{
				_ = webView.EnsureCoreWebView2Async();
				webView.CoreWebView2?.Navigate(input);
			}
		};
		txtUrl.KeyDown += (_, e) =>
		{
			if (e.KeyCode == Keys.Enter) btnGo.PerformClick();
		};

		Controls.Add(webView);
		Controls.Add(header);

		KeyPreview = true;
		KeyDown += (_, e) =>
		{
			if (e.KeyCode == Keys.Escape) Close();
		};
	}
}
