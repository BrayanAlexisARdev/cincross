using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CINCROSS;

partial class Form1
{
	private const int LighthouseWidth = 36;

	private BufferedPanel? lighthouseBar;
	private Timer? _lighthouseTimer;
	private int _lighthouseState;
	private float _lighthousePhase;
	private int _lighthouseBlinkTicks;
	private bool _lighthouseDirty = true;

	private int ContentWidth => base.Width - LighthouseWidth;

	private void SetupLighthouse()
	{
		lighthouseBar = new BufferedPanel
		{
			Name = "lighthouseBar",
			Dock = DockStyle.Left,
			Width = LighthouseWidth,
			BackColor = Color.FromArgb(58, 58, 62)
		};
		lighthouseBar.Paint += LighthouseBar_Paint;
		base.Controls.Add(lighthouseBar);

		_lighthouseTimer = new Timer { Interval = 60 };
		_lighthouseTimer.Tick += delegate
		{
			if (_lighthouseState == 2)
			{
				_lighthousePhase += 0.1f;
				_lighthouseBlinkTicks++;
				if (_lighthouseBlinkTicks >= 84)
				{
					SetLighthouseState(0);
					return;
				}
				_lighthouseDirty = true;
			}
			if (_lighthouseDirty)
			{
				_lighthouseDirty = false;
				lighthouseBar?.Invalidate();
			}
		};
		_lighthouseTimer.Start();
	}

	private void SetLighthouseState(int state)
	{
		if (_lighthouseState == state) return;
		_lighthouseState = state;
		_lighthousePhase = 0f;
		_lighthouseBlinkTicks = 0;
		_lighthouseDirty = true;
	}

	private void LighthouseBar_Paint(object? sender, PaintEventArgs e)
	{
		if (sender is not Control c) return;
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = c.ClientRectangle;

		using (var body = new LinearGradientBrush(new RectangleF(0f, 0f, c.Width, c.Height),
			Color.FromArgb(98, 98, 104), Color.FromArgb(50, 50, 54), LinearGradientMode.Vertical))
			g.FillRectangle(body, r);
		using (var edge = new Pen(Color.FromArgb(160, 26, 26, 28), 1f))
			g.DrawLine(edge, c.Width - 1, 0, c.Width - 1, c.Height);
		using (var hi = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
			g.DrawLine(hi, c.Width - 2, 0, c.Width - 2, c.Height);

		Rectangle slot = new Rectangle(3, 4, c.Width - 7, c.Height - 8);
		using (var recess = new SolidBrush(Color.FromArgb(24, 24, 27)))
			g.FillRectangle(recess, slot);
		using (var rim = new Pen(Color.FromArgb(90, 0, 0, 0), 1f))
			g.DrawRectangle(rim, slot.X, slot.Y, slot.Width - 1, slot.Height - 1);

		Color accent = GetAccentColor();
		int alpha;
		Color lampBase;
		switch (_lighthouseState)
		{
			case 1:
				alpha = 235;
				lampBase = Color.FromArgb(alpha, 206, 206, 212);
				break;
			case 2:
				alpha = ((_lighthousePhase % 2f) < 1f) ? 255 : 20;
				lampBase = Color.FromArgb(alpha, accent);
				break;
			default:
				alpha = 0;
				lampBase = Color.FromArgb(70, 84, 84, 90);
				break;
		}

		Rectangle lamp = new Rectangle(slot.X + 2, slot.Y + 2, slot.Width - 4, slot.Height - 4);
		using (var glow = new LinearGradientBrush(lamp,
			ControlPaint.Light(lampBase, 0.15f), ControlPaint.Dark(lampBase, 0.25f), LinearGradientMode.Vertical))
			g.FillRectangle(glow, lamp);
		if (alpha > 0)
		{
			int coreW = Math.Max(2, lamp.Width / 3);
			using var core = new SolidBrush(Color.FromArgb(Math.Min(255, alpha / 2 + 60), Color.White));
			g.FillRectangle(core, lamp.X + (lamp.Width - coreW) / 2, lamp.Y + 2, coreW, lamp.Height - 4);
		}

		using (var rivet = new SolidBrush(Color.FromArgb(150, 122, 122, 128)))
		using (var rivetDark = new SolidBrush(Color.FromArgb(120, 18, 18, 20)))
		{
			int rx = c.Width / 2 - 2;
			for (int i = 0; i < 2; i++)
			{
				int ry = i == 0 ? 16 : c.Height - 16;
				g.FillEllipse(rivetDark, rx, ry - 1, 4, 4);
				g.FillEllipse(rivet, rx, ry - 2, 4, 4);
			}
		}
	}
}
