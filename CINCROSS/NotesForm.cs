using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CINCROSS;

/// <summary>
/// Ventana lateral NOTES. Permite escribir y guardar hasta 10 notas, cada una con
/// un nombre y un icono tipo cassette (temática radio). Carga y guardado simple.
/// Comparte la estética "radio/steel" de la aplicación principal.
/// </summary>
public class NotesForm : Form
{
	private const int FormWidth = 500;
	private const int SlotsWidth = 176;
	private const int SlotButtonWidth = 140;
	private const int CornerRadius = 12;

	private static readonly Color SteelTop = Color.FromArgb(76, 76, 81);
	private static readonly Color SteelBottom = Color.FromArgb(22, 22, 25);
	private static readonly Color BrassLight = Color.FromArgb(224, 186, 108);
	private static readonly Color BrassDark = Color.FromArgb(112, 86, 42);
	private static readonly Color BrassLine = Color.FromArgb(150, 196, 164, 92);
	private static readonly Color Recess = Color.FromArgb(14, 14, 16);
	private static readonly Color ScreenBg = Color.FromArgb(18, 18, 21);
	private static readonly Color TextMain = Color.FromArgb(226, 226, 230);
	private static readonly Color TextDim = Color.FromArgb(150, 150, 156);
	private static readonly Color Accent = Color.FromArgb(255, 205, 84);
	private static readonly Color Warn = Color.FromArgb(230, 120, 110);
	private static readonly Color ReelOn = Color.FromArgb(120, 200, 255);

	[DllImport("Gdi32.dll")]
	private static extern nint CreateRoundRectRgn(int left, int top, int right, int bottom, int w, int h);

	[DllImport("Gdi32.dll")]
	private static extern bool DeleteObject(nint hObject);

	private readonly NotesData _data;
	private int _selectedIndex = -1;

	private readonly Panel _header;
	private readonly Panel _bottomPanel;
	private readonly Panel _slotsHost;
	private readonly FlowLayoutPanel _slotsFlow;
	private readonly Panel _editorPanel;
	private readonly Panel _nameRow;
	private readonly Panel _contentHost;
	private readonly Panel _btnRow;
	private readonly TextBox _txtName;
	private readonly TextBox _txtContent;
	private readonly Label _lblStatus;
	private readonly Label _lblCounter;
	private readonly Button[] _slotButtons = new Button[NotesData.MaxNotes];

	private readonly Font _headerFont = new Font("Segoe UI", 11f, FontStyle.Bold);
	private readonly Font _smallFont = new Font("Segoe UI", 7f, FontStyle.Bold);
	private readonly Font _slotFont = new Font("Segoe UI", 8f, FontStyle.Bold);

	public NotesForm(int targetHeight)
	{
		_data = NotesData.Load();

		Text = "NOTES";
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.Manual;
		Size = new Size(FormWidth, Math.Max(500, targetHeight));
		MinimumSize = new Size(FormWidth, 500);
		BackColor = SteelBottom;
		ShowInTaskbar = false;
		KeyPreview = true;
		DoubleBuffered = true;

		_header = new Panel { Dock = DockStyle.Top, Height = 44, Name = "header" };
		_header.Paint += HeaderPaint;

		_bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 34, Name = "bottomPanel" };
		_bottomPanel.Paint += BottomPanelPaint;

		_lblStatus = new Label
		{
			Dock = DockStyle.Left,
			Width = FormWidth - 96,
			ForeColor = TextDim,
			Font = _smallFont,
			TextAlign = ContentAlignment.MiddleLeft,
			Padding = new Padding(12, 0, 0, 0),
			BackColor = Color.Transparent,
			Text = "Selecciona o crea una nota"
		};

		_lblCounter = new Label
		{
			Dock = DockStyle.Right,
			Width = 90,
			ForeColor = BrassLight,
			Font = _smallFont,
			TextAlign = ContentAlignment.MiddleRight,
			Padding = new Padding(0, 0, 10, 0),
			BackColor = Color.Transparent,
			Text = "0/10"
		};

		Button btnClose = new Button
		{
			Text = "SALIR",
			Dock = DockStyle.Right,
			Width = 78,
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0 },
			BackColor = Color.Transparent,
			ForeColor = Color.White,
			Font = _smallFont,
			UseVisualStyleBackColor = false,
			Cursor = Cursors.Hand
		};
		btnClose.Paint += BrassButtonPaint;
		btnClose.Click += (_, _) => Close();

		_bottomPanel.Controls.Add(_lblStatus);
		_bottomPanel.Controls.Add(_lblCounter);
		_bottomPanel.Controls.Add(btnClose);

		_slotsHost = new Panel
		{
			Dock = DockStyle.Left,
			Width = SlotsWidth,
			Name = "slotsHost",
			Padding = new Padding(7, 6, 5, 6)
		};
		_slotsHost.Paint += SlotsHostPaint;

		_slotsFlow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoScroll = true,
			BackColor = ScreenBg,
			Padding = new Padding(6, 6, 4, 6)
		};
		_slotsHost.Controls.Add(_slotsFlow);

		_editorPanel = new Panel
		{
			Dock = DockStyle.Fill,
			Name = "editorPanel",
			Padding = new Padding(14, 10, 14, 12)
		};
		_editorPanel.Paint += EditorPanelPaint;

		_nameRow = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
		Label lblName = new Label
		{
			Dock = DockStyle.Top,
			Height = 16,
			Text = "NOMBRE DE LA NOTA",
			ForeColor = BrassLight,
			Font = _smallFont,
			BackColor = Color.Transparent
		};
		_txtName = new TextBox
		{
			Dock = DockStyle.Fill,
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = ScreenBg,
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 11f, FontStyle.Bold)
		};
		_nameRow.Controls.Add(_txtName);
		_nameRow.Controls.Add(lblName);

		_contentHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
		Label lblText = new Label
		{
			Dock = DockStyle.Top,
			Height = 18,
			Text = "TEXTO",
			ForeColor = BrassLight,
			Font = _smallFont,
			BackColor = Color.Transparent
		};
		_txtContent = new TextBox
		{
			Dock = DockStyle.Fill,
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = ScreenBg,
			ForeColor = TextMain,
			Font = new Font("Segoe UI", 12f),
			Multiline = true,
			WordWrap = true,
			ScrollBars = ScrollBars.Vertical,
			AcceptsReturn = true,
			AcceptsTab = true
		};
		_contentHost.Controls.Add(_txtContent);
		_contentHost.Controls.Add(lblText);

		_btnRow = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = Color.Transparent };
		Button btnSave = MakePlateButton("GUARDAR", 120);
		btnSave.Dock = DockStyle.Left;
		Button btnNew = MakePlateButton("NUEVA", 90);
		btnNew.Dock = DockStyle.Left;
		Button btnDelete = MakePlateButton("BORRAR", 90);
		btnDelete.Dock = DockStyle.Right;
		btnSave.Click += (_, _) => SaveNote();
		btnNew.Click += (_, _) => NewNote();
		btnDelete.Click += (_, _) => DeleteNote();
		_btnRow.Controls.Add(btnNew);
		_btnRow.Controls.Add(btnSave);
		_btnRow.Controls.Add(btnDelete);

		_editorPanel.Controls.Add(_contentHost);
		_editorPanel.Controls.Add(_btnRow);
		_editorPanel.Controls.Add(_nameRow);

		Controls.Add(_editorPanel);
		Controls.Add(_slotsHost);
		Controls.Add(_bottomPanel);
		Controls.Add(_header);

		KeyDown += (_, e) =>
		{
			if (e.KeyCode == Keys.Escape) Close();
		};

		BuildSlots();
		RefreshSlots();
		UpdateCounter();
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		ApplyRoundedRegion();
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		if (IsHandleCreated) ApplyRoundedRegion();
	}

	private void ApplyRoundedRegion()
	{
		nint hrgn = CreateRoundRectRgn(0, 0, Width, Height, CornerRadius, CornerRadius);
		Region = Region.FromHrgn(hrgn);
		DeleteObject(hrgn);
	}

	private Button MakePlateButton(string text, int width)
	{
		Button btn = new Button
		{
			Text = text,
			Width = width,
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent },
			BackColor = Color.Transparent,
			ForeColor = Color.White,
			Font = _smallFont,
			UseVisualStyleBackColor = false,
			Cursor = Cursors.Hand,
			Margin = new Padding(0, 0, 6, 0),
			UseMnemonic = false
		};
		btn.Paint += BrassButtonPaint;
		return btn;
	}

	private void BuildSlots()
	{
		for (int i = 0; i < NotesData.MaxNotes; i++)
		{
			int index = i;
			Button slot = new Button
			{
				Name = "noteSlot" + i,
				Text = $"NOTA {i + 1:00}",
				Width = SlotButtonWidth,
				Height = 34,
				FlatStyle = FlatStyle.Flat,
				FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent },
				BackColor = ScreenBg,
				ForeColor = TextMain,
				Font = _slotFont,
				UseVisualStyleBackColor = false,
				TextAlign = ContentAlignment.MiddleLeft,
				Cursor = Cursors.Hand,
				Margin = new Padding(0, 0, 0, 4),
				Tag = i,
				UseMnemonic = false
			};
			slot.Paint += SlotPaint;
			slot.Click += (_, _) => SelectSlot(index);
			_slotButtons[i] = slot;
			_slotsFlow.Controls.Add(slot);
		}
	}

	private void RefreshSlots()
	{
		for (int i = 0; i < _slotButtons.Length; i++)
		{
			NoteItem note = _data.Notes[i];
			string label = NotesData.IsEmpty(note) ? $"NOTA {i + 1:00}" : note.Name.Trim();
			if (label.Length == 0) label = $"NOTA {i + 1:00}";
			_slotButtons[i].Text = label;
			_slotButtons[i].Invalidate();
		}
		UpdateCounter();
	}

	private void UpdateCounter()
	{
		int used = 0;
		foreach (NoteItem note in _data.Notes)
			if (!NotesData.IsEmpty(note)) used++;
		_lblCounter.Text = $"{used}/{NotesData.MaxNotes}";
	}

	private void SelectSlot(int index)
	{
		if (index < 0 || index >= NotesData.MaxNotes) return;
		_selectedIndex = index;
		NoteItem note = _data.Notes[index];
		_txtName.Text = note.Name;
		_txtContent.Text = note.Content;
		RefreshSlots();
		SetStatus(NotesData.IsEmpty(note)
			? $"NOTA {index + 1:00} · VACÍA"
			: $"NOTA {index + 1:00} · CARGADA", Accent);
		_txtName.Focus();
	}

	private void SaveNote()
	{
		string name = _txtName.Text.Trim();
		string content = _txtContent.Text;

		if (name.Length == 0 && content.Trim().Length == 0)
		{
			SetStatus("ESCRIBE ALGO ANTES DE GUARDAR", Warn);
			_txtContent.Focus();
			return;
		}

		if (_selectedIndex < 0 || _selectedIndex >= NotesData.MaxNotes)
		{
			int empty = _data.Notes.FindIndex(n => NotesData.IsEmpty(n));
			if (empty < 0)
			{
				SetStatus("LAS 10 NOTAS ESTÁN LLENAS", Warn);
				return;
			}
			_selectedIndex = empty;
		}

		if (name.Length == 0) name = $"NOTA {_selectedIndex + 1:00}";
		_data.Notes[_selectedIndex] = new NoteItem
		{
			Name = name,
			Content = content,
			SavedAt = DateTime.Now
		};
		_data.Save();
		RefreshSlots();
		SetStatus($"NOTA {_selectedIndex + 1:00} GUARDADA", Accent);
	}

	private void NewNote()
	{
		_selectedIndex = -1;
		_txtName.Clear();
		_txtContent.Clear();
		RefreshSlots();
		SetStatus("NUEVA NOTA · escribe y guarda", TextDim);
		_txtName.Focus();
	}

	private void DeleteNote()
	{
		if (_selectedIndex < 0 || _selectedIndex >= NotesData.MaxNotes)
		{
			SetStatus("NO HAY NOTA SELECCIONADA", Warn);
			return;
		}
		_data.Notes[_selectedIndex] = new NoteItem();
		_data.Save();
		_txtName.Clear();
		_txtContent.Clear();
		RefreshSlots();
		SetStatus($"NOTA {_selectedIndex + 1:00} BORRADA", Warn);
	}

	private void SetStatus(string text, Color color)
	{
		_lblStatus.ForeColor = color;
		_lblStatus.Text = text;
	}

	private void HeaderPaint(object? sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = _header.ClientRectangle;

		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);

		using (var top = new Pen(Color.FromArgb(80, 255, 255, 255), 1f))
			g.DrawLine(top, 0, 0, r.Width, 0);
		using (var brass = new Pen(BrassLine, 1f))
			g.DrawLine(brass, 0, r.Height - 1, r.Width, r.Height - 1);

		using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		RectangleF textRect = new RectangleF(0, 0, r.Width, r.Height);
		using (var glow = new SolidBrush(Color.FromArgb(70, Accent)))
		{
			g.DrawString("NOTES", _headerFont, glow, new RectangleF(1, 1, r.Width, r.Height), fmt);
			g.DrawString("NOTES", _headerFont, glow, new RectangleF(-1, -1, r.Width, r.Height), fmt);
		}
		using (var ink = new SolidBrush(Color.FromArgb(238, 238, 242)))
			g.DrawString("NOTES", _headerFont, ink, textRect, fmt);

		using var right = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
		using (var small = new SolidBrush(BrassLight))
			g.DrawString("CARGA / GUARDADO", _smallFont, small, new RectangleF(0, 0, r.Width - 16, r.Height), right);

		DrawCornerScrews(g, r);
	}

	private void SlotsHostPaint(object? sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = _slotsHost.ClientRectangle;

		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);

		Rectangle screen = _slotsFlow.Bounds;
		screen.Inflate(2, 2);
		using (var rec = new SolidBrush(Recess))
			g.FillRectangle(rec, screen);
		using (var brass = new Pen(Color.FromArgb(140, BrassDark), 1.2f))
			g.DrawRectangle(brass, screen.X, screen.Y, screen.Width - 1, screen.Height - 1);
		using (var hi = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
			g.DrawLine(hi, screen.X + 1, screen.Bottom - 2, screen.Right - 1, screen.Bottom - 2);
	}

	private void EditorPanelPaint(object? sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = _editorPanel.ClientRectangle;

		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);
		using (var brass = new Pen(Color.FromArgb(90, BrassDark), 1f))
			g.DrawLine(brass, 0, 0, 0, r.Height);
	}

	private void BottomPanelPaint(object? sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = _bottomPanel.ClientRectangle;

		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);
		using (var brass = new Pen(BrassLine, 1f))
			g.DrawLine(brass, 0, 0, r.Width, 0);
	}

	private void SlotPaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Button b) return;
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

		bool selected = b.Tag is int idx && idx == _selectedIndex;
		RectangleF rect = new RectangleF(0.5f, 0.5f, b.Width - 1.5f, b.Height - 1.5f);
		using (var path = RoundedPath(rect, 6f))
		{
			using (var fill = new LinearGradientBrush(rect,
				selected ? Color.FromArgb(64, 64, 70) : Color.FromArgb(30, 30, 34),
				selected ? Color.FromArgb(36, 36, 41) : Color.FromArgb(18, 18, 21),
				LinearGradientMode.Vertical))
				g.FillPath(fill, path);
			if (selected)
				using (var glow = new Pen(Color.FromArgb(120, Accent), 1.6f))
					g.DrawPath(glow, path);
			using (var brass = new Pen(selected ? Color.FromArgb(220, BrassLight) : Color.FromArgb(150, BrassDark), 1f))
				g.DrawPath(brass, path);
		}

		RectangleF icon = new RectangleF(rect.X + 5, rect.Y + (rect.Height - 20) / 2f, 26, 20);
		DrawReelIcon(g, icon, selected ? ReelOn : BrassLight);

		RectangleF textRect = new RectangleF(icon.Right + 6, rect.Y, rect.Width - icon.Width - 14, rect.Height);
		Color textColor = selected ? Color.White : TextMain;
		TextRenderer.DrawText(g, b.Text, b.Font, Rectangle.Round(textRect), textColor,
			TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
	}

	private static void DrawReelIcon(Graphics g, RectangleF r, Color accent)
	{
		using (var path = RoundedPath(r, 3f))
		{
			using (var fill = new LinearGradientBrush(r, Color.FromArgb(58, 58, 63), Color.FromArgb(24, 24, 27), LinearGradientMode.Vertical))
				g.FillPath(fill, path);
			using (var rim = new Pen(Color.FromArgb(160, accent), 1f))
				g.DrawPath(rim, path);
		}
		float cy = r.Y + r.Height / 2f;
		float rr = r.Height * 0.22f;
		float left = r.X + r.Width * 0.30f;
		float right = r.X + r.Width * 0.70f;
		using (var reel = new Pen(accent, 1.2f))
		using (var hub = new SolidBrush(Color.FromArgb(12, 12, 14)))
		{
			g.DrawEllipse(reel, left - rr, cy - rr, rr * 2f, rr * 2f);
			g.DrawEllipse(reel, right - rr, cy - rr, rr * 2f, rr * 2f);
			g.FillEllipse(hub, left - rr * 0.35f, cy - rr * 0.35f, rr * 0.7f, rr * 0.7f);
			g.FillEllipse(hub, right - rr * 0.35f, cy - rr * 0.35f, rr * 0.7f, rr * 0.7f);
		}
		using (var tape = new Pen(Color.FromArgb(120, accent), 1f))
			g.DrawLine(tape, left, cy, right, cy);
	}

	private static void BrassButtonPaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Button b) return;
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = b.ClientRectangle;

		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);
		using (var brass = new Pen(BrassLine, 1f))
			g.DrawRectangle(brass, 1, 1, r.Width - 3, r.Height - 3);
		using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		using (var ink = new SolidBrush(Color.White))
			g.DrawString(b.Text, b.Font, ink, new RectangleF(0, 0, r.Width, r.Height), fmt);
	}

	private static GraphicsPath RoundedPath(RectangleF r, float radius)
	{
		float d = radius * 2f;
		GraphicsPath path = new GraphicsPath();
		if (r.Width <= d || r.Height <= d)
		{
			path.AddRectangle(r);
			return path;
		}
		path.AddArc(r.X, r.Y, d, d, 180, 90);
		path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
		path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
		path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
		path.CloseFigure();
		return path;
	}

	private static LinearGradientBrush MetalBrush(RectangleF r)
	{
		return new LinearGradientBrush(r, SteelTop, SteelBottom, LinearGradientMode.Vertical);
	}

	private static void DrawBrushed(Graphics g, Rectangle r)
	{
		using var pen = new Pen(Color.FromArgb(18, 255, 255, 255), 1f);
		for (int y = r.Y + 3; y < r.Bottom; y += 4)
			g.DrawLine(pen, r.X, y, r.Right, y);
	}

	private static void DrawScrew(Graphics g, float cx, float cy)
	{
		using (var body = new SolidBrush(Color.FromArgb(120, 120, 126)))
			g.FillEllipse(body, cx - 3f, cy - 3f, 6f, 6f);
		using (var shade = new SolidBrush(Color.FromArgb(60, 20, 20, 22)))
			g.FillEllipse(shade, cx - 3f, cy, 6f, 3f);
		using (var rim = new Pen(Color.FromArgb(150, 190, 158, 90), 1f))
			g.DrawEllipse(rim, cx - 3f, cy - 3f, 6f, 6f);
		using (var slot = new Pen(Color.FromArgb(45, 45, 48), 1.2f))
			g.DrawLine(slot, cx - 2f, cy, cx + 2f, cy);
	}

	private static void DrawCornerScrews(Graphics g, Rectangle r)
	{
		float inset = 5f;
		DrawScrew(g, r.X + inset, r.Y + inset);
		DrawScrew(g, r.Right - inset, r.Y + inset);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_headerFont.Dispose();
			_smallFont.Dispose();
			_slotFont.Dispose();
		}
		base.Dispose(disposing);
	}
}
