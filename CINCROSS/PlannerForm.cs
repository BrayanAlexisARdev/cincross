using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CINCROSS;

/// <summary>
/// Ventana lateral PLANNER. Reutiliza el seteo de horas/minutos y la mecánica del
/// TIMER (setear y poner en marcha) directamente en esta ventana, pero organizado
/// en paquetes de TRABAJO y ESTUDIO con sus intervalos y nombres.
/// Comparte la estética "radio/steel" de la aplicación principal.
/// </summary>
public class PlannerForm : Form
{
	private enum Mode { Work, Study }

	private const int FormWidth = 460;
	private const int CornerRadius = 12;

	private static readonly Color SteelTop = Color.FromArgb(76, 76, 81);
	private static readonly Color SteelBottom = Color.FromArgb(22, 22, 25);
	private static readonly Color BrassLight = Color.FromArgb(224, 186, 108);
	private static readonly Color BrassDark = Color.FromArgb(112, 86, 42);
	private static readonly Color BrassLine = Color.FromArgb(150, 196, 164, 92);
	private static readonly Color Recess = Color.FromArgb(8, 8, 10);
	private static readonly Color ScreenBg = Color.FromArgb(12, 12, 15);
	private static readonly Color TextMain = Color.FromArgb(226, 226, 230);
	private static readonly Color TextDim = Color.FromArgb(150, 150, 156);
	private static readonly Color WorkAccent = Color.FromArgb(255, 150, 60);
	private static readonly Color StudyAccent = Color.FromArgb(90, 190, 255);
	private static readonly Color Warn = Color.FromArgb(230, 120, 110);

	[DllImport("Gdi32.dll")]
	private static extern nint CreateRoundRectRgn(int left, int top, int right, int bottom, int w, int h);

	[DllImport("Gdi32.dll")]
	private static extern bool DeleteObject(nint hObject);

	private Mode _mode = Mode.Work;
	private bool _running;
	private bool _inBreak;
	private int _focusSeconds;
	private int _breakSeconds;
	private int _remaining;
	private int _sessions;
	private string _method = "DEEP WORK";

	private readonly Panel _header;
	private readonly Panel _modeBar;
	private readonly Panel _screenPanel;
	private readonly Panel _configPanel;
	private readonly Panel _bottomPanel;
	private readonly Label _lblTime;
	private readonly Label _lblPhase;
	private readonly Label _lblMethod;
	private readonly Label _lblStatus;
	private readonly Label _lblPackages;
	private readonly Button _btnWork;
	private readonly Button _btnStudy;
	private readonly Button[] _packageButtons = new Button[3];
	private readonly NumericUpDown _numHours;
	private readonly NumericUpDown _numMinutes;
	private readonly NumericUpDown _numBreak;
	private readonly Button _btnStart;
	private readonly Button _btnPause;
	private readonly Button _btnReset;
	private readonly Timer _ticker;

	private readonly Font _headerFont = new Font("Segoe UI", 11f, FontStyle.Bold);
	private readonly Font _smallFont = new Font("Segoe UI", 7f, FontStyle.Bold);
	private readonly Font _packTitleFont = new Font("Segoe UI", 8f, FontStyle.Bold);
	private readonly Font _packSubFont = new Font("Segoe UI", 6.5f, FontStyle.Bold);
	private readonly Font _monoFont = new Font("Consolas", 34f, FontStyle.Bold);
	private readonly Font _phaseFont = new Font("Segoe UI", 10f, FontStyle.Bold);
	private readonly Font _modeFont = new Font("Segoe UI", 11f, FontStyle.Bold);

	// Paquetes de trabajo: bloques largos con pausas cortas.
	private static readonly (int focus, int brk, string name)[] WorkPackages =
	{
		(90, 20, "DEEP WORK"),
		(60, 15, "BLOQUE 60"),
		(50, 10, "SPRINT 50")
	};

	// Paquetes de estudio: bloques con descansos intermedios frecuentes.
	private static readonly (int focus, int brk, string name)[] StudyPackages =
	{
		(25, 5, "POMODORO"),
		(45, 15, "ESTUDIO 45"),
		(30, 5, "REPASO 30")
	};

	public PlannerForm(int targetHeight)
	{
		Text = "PLANNER";
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.Manual;
		Size = new Size(FormWidth, Math.Max(560, targetHeight));
		MinimumSize = new Size(FormWidth, 560);
		BackColor = SteelBottom;
		ShowInTaskbar = false;
		KeyPreview = true;
		DoubleBuffered = true;

		_ticker = new Timer { Interval = 1000 };
		_ticker.Tick += (_, _) => Tick();

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
			Text = "Elige un paquete o setea el tiempo"
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
		_bottomPanel.Controls.Add(btnClose);

		_modeBar = new Panel { Dock = DockStyle.Top, Height = 48, Name = "modeBar" };
		_modeBar.Paint += ModeBarPaint;
		_btnWork = MakeModeButton("WORK");
		_btnWork.Click += (_, _) => SwitchMode(Mode.Work);
		_btnStudy = MakeModeButton("STUDY");
		_btnStudy.Click += (_, _) => SwitchMode(Mode.Study);
		_modeBar.Controls.Add(_btnWork);
		_modeBar.Controls.Add(_btnStudy);

		_screenPanel = new Panel { Dock = DockStyle.Top, Height = 150, Name = "screenPanel" };
		_screenPanel.Paint += ScreenPanelPaint;

		_lblTime = new Label
		{
			Dock = DockStyle.Top,
			Height = 78,
			Text = "00:00",
			ForeColor = Color.White,
			Font = _monoFont,
			TextAlign = ContentAlignment.MiddleCenter,
			BackColor = Color.Transparent
		};
		_lblPhase = new Label
		{
			Dock = DockStyle.Top,
			Height = 26,
			Text = "TRABAJO",
			ForeColor = WorkAccent,
			Font = _phaseFont,
			TextAlign = ContentAlignment.MiddleCenter,
			BackColor = Color.Transparent
		};
		_lblMethod = new Label
		{
			Dock = DockStyle.Fill,
			Text = "",
			ForeColor = TextDim,
			Font = _smallFont,
			TextAlign = ContentAlignment.MiddleCenter,
			BackColor = Color.Transparent
		};
		_screenPanel.Controls.Add(_lblMethod);
		_screenPanel.Controls.Add(_lblPhase);
		_screenPanel.Controls.Add(_lblTime);

		_configPanel = new Panel { Dock = DockStyle.Fill, Name = "configPanel" };
		_configPanel.Paint += ConfigPanelPaint;

		Label lblDuration = new Label
		{
			Text = "SETEAR TIEMPO (HORAS / MINUTOS)",
			ForeColor = BrassLight,
			Font = _smallFont,
			Location = new Point(16, 12),
			AutoSize = true,
			BackColor = Color.Transparent
		};
		Label lblHours = new Label { Text = "HORAS", ForeColor = TextDim, Font = _smallFont, Location = new Point(16, 38), AutoSize = true, BackColor = Color.Transparent };
		Label lblMinutes = new Label { Text = "MINUTOS", ForeColor = TextDim, Font = _smallFont, Location = new Point(104, 38), AutoSize = true, BackColor = Color.Transparent };
		Label lblBreak = new Label { Text = "DESCANSO (MIN)", ForeColor = TextDim, Font = _smallFont, Location = new Point(192, 38), AutoSize = true, BackColor = Color.Transparent };

		_numHours = MakeSpin(16, 56, 0, 12);
		_numMinutes = MakeSpin(104, 56, 0, 59);
		_numBreak = MakeSpin(192, 56, 0, 59);
		_numHours.ValueChanged += (_, _) => OnManualChanged();
		_numMinutes.ValueChanged += (_, _) => OnManualChanged();
		_numBreak.ValueChanged += (_, _) => OnManualChanged();

		_lblPackages = new Label
		{
			Text = "PAQUETES DE TRABAJO",
			ForeColor = BrassLight,
			Font = _smallFont,
			Location = new Point(16, 100),
			AutoSize = true,
			BackColor = Color.Transparent
		};

		for (int i = 0; i < 3; i++)
		{
			Button p = new Button
			{
				Width = 132,
				Height = 54,
				FlatStyle = FlatStyle.Flat,
				FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent },
				BackColor = Color.Transparent,
				ForeColor = TextMain,
				Font = _smallFont,
				UseVisualStyleBackColor = false,
				Cursor = Cursors.Hand,
				Location = new Point(16 + i * 140, 120),
				Tag = i,
				UseMnemonic = false
			};
			int slot = i;
			p.Paint += PackagePaint;
			p.Click += (_, _) => ApplyPackage(slot);
			_packageButtons[i] = p;
			_configPanel.Controls.Add(p);
		}

		Label lblControls = new Label
		{
			Text = "CONTROLES",
			ForeColor = BrassLight,
			Font = _smallFont,
			Location = new Point(16, 186),
			AutoSize = true,
			BackColor = Color.Transparent
		};

		_btnStart = MakePlateButton("INICIAR", 132);
		_btnStart.Location = new Point(16, 208);
		_btnStart.Click += (_, _) => StartTimer();
		_btnPause = MakePlateButton("PAUSA", 132);
		_btnPause.Location = new Point(156, 208);
		_btnPause.Click += (_, _) => PauseTimer();
		_btnReset = MakePlateButton("REINICIAR", 132);
		_btnReset.Location = new Point(296, 208);
		_btnReset.Click += (_, _) => ResetTimer(true);

		_configPanel.Controls.Add(lblDuration);
		_configPanel.Controls.Add(lblHours);
		_configPanel.Controls.Add(lblMinutes);
		_configPanel.Controls.Add(lblBreak);
		_configPanel.Controls.Add(_numHours);
		_configPanel.Controls.Add(_numMinutes);
		_configPanel.Controls.Add(_numBreak);
		_configPanel.Controls.Add(_lblPackages);
		_configPanel.Controls.Add(lblControls);
		_configPanel.Controls.Add(_btnStart);
		_configPanel.Controls.Add(_btnPause);
		_configPanel.Controls.Add(_btnReset);

		Controls.Add(_configPanel);
		Controls.Add(_bottomPanel);
		Controls.Add(_screenPanel);
		Controls.Add(_modeBar);
		Controls.Add(_header);

		KeyDown += (_, e) =>
		{
			if (e.KeyCode == Keys.Escape) Close();
		};

		LayoutModeButtons();
		SwitchMode(Mode.Work);
	}

	private Color Accent => _mode == Mode.Work ? WorkAccent : StudyAccent;

	private (int focus, int brk, string name)[] Packages => _mode == Mode.Work ? WorkPackages : StudyPackages;

	private NumericUpDown MakeSpin(int x, int y, int min, int max)
	{
		return new NumericUpDown
		{
			Location = new Point(x, y),
			Width = 76,
			Minimum = min,
			Maximum = max,
			BackColor = ScreenBg,
			ForeColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			TextAlign = HorizontalAlignment.Center
		};
	}

	private Button MakePlateButton(string text, int width)
	{
		Button btn = new Button
		{
			Text = text,
			Width = width,
			Height = 36,
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent },
			BackColor = Color.Transparent,
			ForeColor = Color.White,
			Font = _smallFont,
			UseVisualStyleBackColor = false,
			Cursor = Cursors.Hand,
			UseMnemonic = false
		};
		btn.Paint += BrassButtonPaint;
		return btn;
	}

	private Button MakeModeButton(string text)
	{
		Button btn = new Button
		{
			Text = text,
			Height = 38,
			Width = (FormWidth - 24) / 2,
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent },
			BackColor = Color.Transparent,
			ForeColor = Color.White,
			Font = _modeFont,
			UseVisualStyleBackColor = false,
			Cursor = Cursors.Hand,
			Tag = false,
			UseMnemonic = false
		};
		btn.Paint += ModeButtonPaint;
		return btn;
	}

	private void LayoutModeButtons()
	{
		int w = (FormWidth - 24) / 2;
		_btnWork.Width = w;
		_btnStudy.Width = w;
		_btnWork.Location = new Point(12, 5);
		_btnStudy.Location = new Point(12 + w, 5);
	}

	private void SwitchMode(Mode mode)
	{
		_mode = mode;
		_running = false;
		_ticker.Stop();
		_inBreak = false;
		_sessions = 0;

		bool study = mode == Mode.Study;
		_btnWork.Tag = !study;
		_btnStudy.Tag = study;
		_btnWork.Invalidate();
		_btnStudy.Invalidate();
		_lblPackages.Text = study ? "PAQUETES DE ESTUDIO" : "PAQUETES DE TRABAJO";
		_lblPhase.ForeColor = Accent;

		for (int i = 0; i < _packageButtons.Length; i++)
			_packageButtons[i].Invalidate();

		ApplyPackage(0);
		_lblStatus.ForeColor = TextDim;
		_lblStatus.Text = study ? "MODO ESTUDIO · descansos intermedios" : "MODO TRABAJO · bloques largos";
		_screenPanel.Invalidate();
	}

	private void ApplyPackage(int index)
	{
		(int focus, int brk, string name)[] packages = Packages;
		if (index < 0 || index >= packages.Length) return;
		(int focus, int brk, string name) = packages[index];

		_suppressManual = true;
		_numHours.Value = focus / 60;
		_numMinutes.Value = focus % 60;
		_numBreak.Value = brk;
		_suppressManual = false;

		_method = name;
		for (int i = 0; i < _packageButtons.Length; i++)
			_packageButtons[i].Invalidate();

		ResetTimer(false);
		_lblStatus.ForeColor = Accent;
		_lblStatus.Text = $"PAQUETE: / {brk} min pausa";
	}

	private bool _suppressManual;

	private void OnManualChanged()
	{
		if (_suppressManual) return;
		ReadDurations();
		_remaining = _focusSeconds;
		_running = false;
		_ticker.Stop();
		_inBreak = false;
		UpdateDisplay();
		_lblStatus.ForeColor = TextDim;
		_lblStatus.Text = "TIEMPO SETEADO · pulsa INICIAR";
	}

	private void ReadDurations()
	{
		_focusSeconds = (int)(_numHours.Value * 3600 + _numMinutes.Value * 60);
		if (_focusSeconds <= 0) _focusSeconds = 25 * 60;
		_breakSeconds = (int)(_numBreak.Value * 60);
	}

	private void StartTimer()
	{
		if (_running) return;
		if (_remaining <= 0)
		{
			ReadDurations();
			_remaining = _inBreak ? _breakSeconds : _focusSeconds;
		}
		_running = true;
		_ticker.Start();
		_lblStatus.ForeColor = Accent;
		_lblStatus.Text = _inBreak ? "DESCANSO EN MARCHA" : "SESIÓN EN MARCHA";
		UpdateDisplay();
	}

	private void PauseTimer()
	{
		if (!_running) return;
		_running = false;
		_ticker.Stop();
		_lblStatus.ForeColor = TextDim;
		_lblStatus.Text = "EN PAUSA";
	}

	private void ResetTimer(bool announce)
	{
		_running = false;
		_ticker.Stop();
		_inBreak = false;
		ReadDurations();
		_remaining = _focusSeconds;
		if (announce)
		{
			_lblStatus.ForeColor = TextDim;
			_lblStatus.Text = "REINICIADO";
		}
		UpdateDisplay();
	}

	private void Tick()
	{
		if (_remaining > 0) _remaining--;
		UpdateDisplay();
		if (_remaining <= 0) PhaseFinished();
	}

	private void PhaseFinished()
	{
		try { SystemSounds.Asterisk.Play(); } catch (Exception ex) { Logger.Error("PlannerForm.Sound", ex); }

		if (_breakSeconds > 0)
		{
			if (!_inBreak)
			{
				_inBreak = true;
				_remaining = _breakSeconds;
				_lblStatus.ForeColor = Accent;
				_lblStatus.Text = "DESCANSO · estira y respira";
			}
			else
			{
				_inBreak = false;
				_sessions++;
				_remaining = _focusSeconds;
				_lblStatus.ForeColor = Accent;
				_lblStatus.Text = $"SESIÓN {_sessions} COMPLETA · a seguir";
			}
			UpdateDisplay();
		}
		else
		{
			_sessions++;
			_running = false;
			_ticker.Stop();
			_inBreak = false;
			ReadDurations();
			_remaining = _focusSeconds;
			_lblStatus.ForeColor = Accent;
			_lblStatus.Text = "SESIÓN COMPLETADA";
			UpdateDisplay();
		}
	}

	private void UpdateDisplay()
	{
		int s = Math.Max(0, _remaining);
		int h = s / 3600;
		int m = (s % 3600) / 60;
		int sec = s % 60;
		_lblTime.Text = h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m:00}:{sec:00}";

		if (_inBreak)
			_lblPhase.Text = "DESCANSO";
		else
			_lblPhase.Text = _mode == Mode.Work ? "TRABAJO" : "ENFOQUE";
		_lblPhase.ForeColor = _inBreak ? StudyAccent : Accent;

		string kind = _mode == Mode.Work ? "TRABAJO" : "ESTUDIO";
		_lblMethod.Text = $"{_method} · {kind} · Sesiones: {_sessions}";

		_screenPanel.Invalidate();
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
		using (var accent = new Pen(Color.FromArgb(150, Accent), 2f))
			g.DrawLine(accent, 0, r.Height - 2, r.Width, r.Height - 2);

		using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		RectangleF textRect = new RectangleF(0, 0, r.Width, r.Height);
		using (var glow = new SolidBrush(Color.FromArgb(70, Accent)))
		{
			g.DrawString("PLANNER", _headerFont, glow, new RectangleF(1, 1, r.Width, r.Height), fmt);
			g.DrawString("PLANNER", _headerFont, glow, new RectangleF(-1, -1, r.Width, r.Height), fmt);
		}
		using (var ink = new SolidBrush(Color.FromArgb(238, 238, 242)))
			g.DrawString("PLANNER", _headerFont, ink, textRect, fmt);

		using var right = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
		using (var small = new SolidBrush(BrassLight))
			g.DrawString("WORK / STUDY", _smallFont, small, new RectangleF(0, 0, r.Width - 16, r.Height), right);

		DrawCornerScrews(g, r);
	}

	private void ModeBarPaint(object? sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = _modeBar.ClientRectangle;
		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);
	}

	private void ScreenPanelPaint(object? sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = _screenPanel.ClientRectangle;

		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);

		Rectangle screen = r;
		screen.Inflate(-14, -12);
		using (var path = RoundedPath(screen, 8f))
		{
			using (var rec = new LinearGradientBrush(screen, Color.FromArgb(24, 24, 30), Recess, LinearGradientMode.Vertical))
				g.FillPath(rec, path);
			using (var glow = new SolidBrush(Color.FromArgb(28, Accent)))
				g.FillPath(glow, path);
			using (var rim = new Pen(Color.FromArgb(150, BrassDark), 1.2f))
				g.DrawPath(rim, path);
			using (var hi = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
				g.DrawLine(hi, screen.X + 4, screen.Bottom - 3, screen.Right - 4, screen.Bottom - 3);
		}
	}

	private void ConfigPanelPaint(object? sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = _configPanel.ClientRectangle;
		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);
		using (var brass = new Pen(Color.FromArgb(70, BrassDark), 1f))
		{
			g.DrawLine(brass, 16, 92, r.Width - 16, 92);
			g.DrawLine(brass, 16, 178, r.Width - 16, 178);
		}
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

	private void ModeButtonPaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Button b) return;
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		bool study = ReferenceEquals(b, _btnStudy);
		bool selected = b.Tag is bool sel && sel;
		Color tint = study ? StudyAccent : WorkAccent;
		RectangleF rect = new RectangleF(1, 1, b.Width - 2, b.Height - 2);

		using (var path = RoundedPath(Rectangle.Round(rect), 7f))
		{
			if (selected)
			{
				using (var fill = new LinearGradientBrush(rect, ControlPaint.Light(tint, 0.2f), ControlPaint.Dark(tint, 0.35f), LinearGradientMode.Vertical))
					g.FillPath(fill, path);
				using (var halo = new Pen(Color.FromArgb(140, ControlPaint.Light(tint)), 2f))
					g.DrawPath(halo, path);
			}
			else
			{
				using (var fill = new LinearGradientBrush(rect, Color.FromArgb(72, 72, 77), Color.FromArgb(30, 30, 34), LinearGradientMode.Vertical))
					g.FillPath(fill, path);
				using (var rim = new Pen(Color.FromArgb(150, BrassDark), 1f))
					g.DrawPath(rim, path);
			}
		}

		using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		using (var ink = new SolidBrush(selected ? Color.White : Color.FromArgb(190, 195, 195, 200)))
			g.DrawString(b.Text, b.Font, ink, new RectangleF(0, 0, b.Width, b.Height), fmt);
	}

	private void PackagePaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Button b) return;
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Color tint = Accent;
		RectangleF rect = new RectangleF(1, 1, b.Width - 2, b.Height - 2);

		using (var path = RoundedPath(Rectangle.Round(rect), 6f))
		{
			using (var fill = new LinearGradientBrush(rect, Color.FromArgb(66, 66, 71), Color.FromArgb(28, 28, 32), LinearGradientMode.Vertical))
				g.FillPath(fill, path);
			using (var rim = new Pen(Color.FromArgb(160, BrassDark), 1f))
				g.DrawPath(rim, path);
			using (var strip = new SolidBrush(Color.FromArgb(200, tint)))
				g.FillRectangle(strip, rect.X + 2, rect.Y + 4, 4, rect.Height - 8);
		}

		string title = "";
		string interval = "";
		if (b.Tag is int idx)
		{
			(int focus, int brk, string name)[] packages = Packages;
			if (idx >= 0 && idx < packages.Length)
			{
				title = packages[idx].name;
				interval = $"{packages[idx].focus} / {packages[idx].brk} min";
			}
		}

		using var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		using (var ink = new SolidBrush(TextMain))
			g.DrawString(title, _packTitleFont, ink, new RectangleF(0, 8, b.Width, 22), center);
		using (var dim = new SolidBrush(Color.FromArgb(210, tint)))
			g.DrawString(interval, _packSubFont, dim, new RectangleF(0, b.Height - 26, b.Width, 20), center);
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

	private static GraphicsPath RoundedPath(Rectangle r, float radius)
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
			_ticker.Dispose();
			_headerFont.Dispose();
			_smallFont.Dispose();
			_packTitleFont.Dispose();
			_packSubFont.Dispose();
			_monoFont.Dispose();
			_phaseFont.Dispose();
			_modeFont.Dispose();
		}
		base.Dispose(disposing);
	}
}
