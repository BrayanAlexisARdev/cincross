using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CINCROSS;

partial class Form1
{
	private List<string> _aniFiles = new List<string>();

	private int _currentAniIndex;

	private string? _noiseFile;

	private List<Bitmap> _spriteFrames = new List<Bitmap>();

	private int _currentSpriteFrame;

	private Timer? _spriteTimer;

	private bool _isDraggingForm;

	private Point _dragStartOffset;

	private bool _isDraggingVolume;

	private Timer? _slideTimer;
	private List<Bitmap>? _fadeOutFrames;
	private List<Bitmap>? _fadeInFrames;
	private int _fadePhase;
	private int _fadeFrameIndex;
	private Image? _nextCassetteImage;
	private bool _timerBackgroundCached;
	private string? _preFadeDisplayImageLocation;

	private const int PictureBoxWidth = 154;

	private const int PictureBoxHeight = 97;

	private Timer? _eqTimer;

	private float _eqAnimationOffset;

	private bool _eqIdlePainted;

	private const int EqStyleCount = 30;

	private int _eqStyleIndex;

	private void AdvanceEqStyle()
	{
		_eqStyleIndex = (_eqStyleIndex + 1) % EqStyleCount;
		_eqIdlePainted = false;
		pnlEqualizer?.Invalidate();
	}

	private static readonly System.Drawing.Imaging.ImageAttributes _alphaAttrs = new System.Drawing.Imaging.ImageAttributes();

	private static readonly System.Drawing.Imaging.ColorMatrix _alphaMatrix = new System.Drawing.Imaging.ColorMatrix();

	private void pnlEqualizer_Paint(object? sender, PaintEventArgs e)
	{
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		int w = pnlEqualizer.Width;
		int h = pnlEqualizer.Height;

		using (var recess = new SolidBrush(_radioRecess))
			e.Graphics.FillRectangle(recess, 0, 0, w, h);
		using (var brass = new Pen(Color.FromArgb(160, 196, 164, 92), 1f))
			e.Graphics.DrawRectangle(brass, 1, 1, w - 3, h - 3);
		using (var inner = new Pen(Color.FromArgb(70, 0, 0, 0), 1f))
			e.Graphics.DrawRectangle(inner, 2, 2, w - 5, h - 5);

		Color neonColor = _appColor == Color.Transparent || _appColor == Color.Empty ? Color.White : _appColor;

		if (!_isPlaying)
		{
			DrawEqIdle(e.Graphics, w, h, neonColor);
			DrawStationTitleOverlay(e.Graphics, w, h);
			return;
		}

		switch (_eqStyleIndex)
		{
			case 0: DrawEqSoftWave(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 1: DrawEqHeartbeat(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 2: DrawEqScanner(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 3: DrawEqMirrorWings(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 4: DrawEqLedCascade(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 5: DrawEqInterference(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 6: DrawEqRandomJump(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 7: DrawEqSawRamp(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 8: DrawEqBubbles(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 9: DrawEqTopDrip(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 10: DrawEqQuakePeaks(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 11: DrawEqDiamondTravel(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 12: DrawEqSnake(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 13: DrawEqPluckString(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 14: DrawEqDigitalRain(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 15: DrawEqContinuousWave(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 16: DrawEqOrbit(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 17: DrawEqHFaders(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 18: DrawEqStaircase(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 19: DrawEqDualVu(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 20: DrawEqString(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 21: DrawEqConfetti(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 22: DrawEqSquareTrain(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 23: DrawEqBreathingDome(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 24: DrawEqRadar(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 25: DrawEqStrobeChecker(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 26: DrawEqJaws(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 27: DrawEqAnts(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 28: DrawEqCoinStacks(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			case 29: DrawEqChirp(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
			default: DrawEqSoftWave(e.Graphics, w, h, neonColor, _eqAnimationOffset); break;
	}
	}

	private void DrawStationTitleOverlay(Graphics g, int w, int h)
	{
		if (string.IsNullOrEmpty(lblM3uTitle.Text)) return;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = TextRenderingHint.AntiAlias;
		string text = lblM3uTitle.Text;
		Font font = lblM3uTitle.Font;
		SizeF sz = g.MeasureString(text, font);
		float tx = (w - sz.Width) / 2f;
		float ty = (h - sz.Height) / 2f;
		using (var outline = new SolidBrush(Color.Black))
		{
			(float dx, float dy)[] offsets =
			[
				(-1f, 0f), (1f, 0f), (0f, -1f), (0f, 1f),
				(-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f)
			];
			foreach (var (dx, dy) in offsets)
				g.DrawString(text, font, outline, tx + dx, ty + dy);
		}
		using (var core = new SolidBrush(lblM3uTitle.ForeColor))
			g.DrawString(text, font, core, tx, ty);
	}

	private static void GetEqLayout(int w, int barCount, out int gap, out int barWidth)
	{
		gap = 2;
		barWidth = (w - gap * (barCount + 1)) / barCount;
		if (barWidth < 2) barWidth = 2;
	}

	private static float EqRand(float seed)
	{
		float s = MathF.Sin(seed * 127.1f + 311.7f) * 43758.5453f;
		return s - MathF.Floor(s);
	}

	private static void FillRoundRect(Graphics g, Brush brush, int x, int y, int width, int height, int radius)
	{
		if (width < 1 || height < 1) return;
		if (radius * 2 > width) radius = width / 2;
		if (radius * 2 > height) radius = height / 2;
		if (radius < 1) radius = 1;
		using var path = new GraphicsPath();
		path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
		path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
		path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
		path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
		path.CloseFigure();
		g.FillPath(brush, path);
	}

	private static void DrawEqBar(Graphics g, int x, int y, int width, int height, Color color, bool glow, int glowAlpha)
	{
		if (height < 1 || width < 1) return;
		int radius = width > 4 ? 3 : 2;
		if (glow)
		{
			using var glowBrush = new SolidBrush(Color.FromArgb(glowAlpha, color));
			FillRoundRect(g, glowBrush, x - 2, y - 1, width + 4, height + 2, radius);
		}
		using var brush = new SolidBrush(color);
		FillRoundRect(g, brush, x, y, width, height, radius);
	}

	private static void DrawEqIdle(Graphics g, int w, int h, Color c)
	{
		GetEqLayout(w, 13, out int gap, out int bw);
		using var brush = new SolidBrush(Color.FromArgb(60, c));
		for (int i = 0; i < 13; i++)
		{
			int x = gap + i * (bw + gap);
			FillRoundRect(g, brush, x, h - 6, bw, 3, 1);
		}
	}

	private static void DrawEqSoftWave(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 13;
		GetEqLayout(w, count, out int gap, out int bw);
		int maxH = h - 6, minH = 3;
		for (int i = 0; i < count; i++)
		{
			float norm = (MathF.Sin(i / (float)count * MathF.PI * 2f + t) + 1f) * 0.5f;
			int bh = minH + (int)(norm * (maxH - minH));
			int x = gap + i * (bw + gap);
			DrawEqBar(g, x, h - 3 - bh, bw, bh, c, true, 70);
		}
	}

	private static void DrawEqHeartbeat(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 13;
		GetEqLayout(w, count, out int gap, out int bw);
		float p = t * 0.6f;
		p -= MathF.Floor(p);
		float beat = MathF.Max(
			MathF.Exp(-(p - 0.12f) * (p - 0.12f) * 150f),
			0.62f * MathF.Exp(-(p - 0.34f) * (p - 0.34f) * 240f));
		float norm = 0.16f + 0.84f * beat;
		int maxH = h - 6, minH = 3;
		int bh = minH + (int)(norm * (maxH - minH));
		int glowAlpha = 40 + (int)(70 * beat);
		for (int i = 0; i < count; i++)
		{
			int x = gap + i * (bw + gap);
			DrawEqBar(g, x, h - 3 - bh, bw, bh, c, true, glowAlpha);
		}
	}

	private static void DrawEqScanner(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 13;
		GetEqLayout(w, count, out int gap, out int bw);
		int maxH = h - 6, minH = 3;
		float span = count * 2f;
		float ph = t * 2.4f;
		ph -= MathF.Floor(ph / span) * span;
		float pos = ph < count ? ph : span - ph;
		if (pos > count - 1) pos = count - 1;
		using var dim = new SolidBrush(Color.FromArgb(45, c));
		for (int i = 0; i < count; i++)
		{
			int x = gap + i * (bw + gap);
			FillRoundRect(g, dim, x, h - 3 - minH, bw, minH, 1);
			float inten = 1f - MathF.Abs(i - pos) / 3.5f;
			if (inten <= 0f) continue;
			int bh = minH + (int)(inten * inten * (maxH - minH));
			DrawEqBar(g, x, h - 3 - bh, bw, bh, c, true, (int)(110 * inten));
		}
	}

	private static void DrawEqMirrorWings(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 13;
		GetEqLayout(w, count, out int gap, out int bw);
		int maxH = h - 6, minH = 3;
		for (int i = 0; i < count; i++)
		{
			float dist = MathF.Abs(i - (count - 1) / 2f);
			float wave = (MathF.Sin(dist * 0.85f - t * 1.3f) + 1f) * 0.5f;
			float norm = 0.1f + 0.9f * wave;
			int bh = minH + (int)(norm * (maxH - minH));
			int x = gap + i * (bw + gap);
			DrawEqBar(g, x, (h - bh) / 2, bw, bh, c, true, 70);
		}
	}

	private static void DrawEqLedCascade(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 13;
		GetEqLayout(w, count, out int gap, out int bw);
		const int segH = 3, segGap = 1;
		int segCount = (h - 6) / (segH + segGap);
		float scroll = t * 0.9f;
		using var litBrush = new SolidBrush(c);
		using var offBrush = new SolidBrush(Color.FromArgb(38, c));
		for (int i = 0; i < count; i++)
		{
			float norm = (MathF.Sin(i * 0.48f - scroll * 1.05f) + 1f) * 0.5f;
			norm = MathF.Pow(norm, 1.2f);
			int lit = (int)MathF.Round(norm * segCount);
			int x = gap + i * (bw + gap);
			for (int j = 0; j < segCount; j++)
			{
				int yBottom = h - 3 - j * (segH + segGap);
				FillRoundRect(g, j < lit ? litBrush : offBrush, x, yBottom - segH, bw, segH, 1);
			}
		}
	}

	private static void DrawEqInterference(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 19;
		GetEqLayout(w, count, out int gap, out int bw);
		int maxH = h - 6, minH = 3;
		for (int i = 0; i < count; i++)
		{
			float v = MathF.Sin(i * 0.63f + t * 1.25f) + MathF.Sin(i * 0.31f - t * 0.8f);
			float norm = (v + 2f) * 0.25f;
			int bh = minH + (int)(norm * (maxH - minH));
			int x = gap + i * (bw + gap);
			DrawEqBar(g, x, h - 3 - bh, bw, bh, c, false, 0);
		}
	}

	private static void DrawEqRandomJump(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 13;
		GetEqLayout(w, count, out int gap, out int bw);
		int maxH = h - 6, minH = 3;
		float step = MathF.Floor(t * 1.1f);
		for (int i = 0; i < count; i++)
		{
			float r = EqRand(i * 17.31f + step * 57.77f);
			float r2 = EqRand(i * 91.13f + step * 23.7f);
			float norm = 0.08f + 0.92f * (r * 0.7f + r2 * 0.3f);
			int bh = minH + (int)(norm * (maxH - minH));
			int x = gap + i * (bw + gap);
			DrawEqBar(g, x, h - 3 - bh, bw, bh, c, false, 0);
			float peak = EqRand(i * 7.7f + step * 0.53f);
			int capY = h - 3 - minH - (int)(peak * (maxH - minH));
			using var cap = new SolidBrush(Color.FromArgb(160, ControlPaint.Light(c, 0.4f)));
			g.FillRectangle(cap, x, capY, bw, 1);
		}
	}

	private static void DrawEqSawRamp(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 13;
		GetEqLayout(w, count, out int gap, out int bw);
		int maxH = h - 6, minH = 3;
		using var b = new SolidBrush(c);
		for (int i = 0; i < count; i++)
		{
			float s = i / (float)count * 2.5f + t * 0.55f;
			s -= MathF.Floor(s);
			int bh = minH + (int)(s * (maxH - minH));
			int x = gap + i * (bw + gap);
			g.FillRectangle(b, x, h - 3 - bh, bw, bh);
		}
	}

	private static void DrawEqBubbles(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 11;
		GetEqLayout(w, count, out int gap, out int bw);
		using var bubbleBrush = new SolidBrush(Color.FromArgb(210, c));
		using var trailBrush = new SolidBrush(Color.FromArgb(70, c));
		for (int i = 0; i < count; i++)
		{
			float speed = 0.5f + EqRand(i * 3.71f) * 0.9f;
			float prog = EqRand(i * 9.13f) + t * speed * 0.33f;
			prog -= MathF.Floor(prog);
			float size = 2f + EqRand(i * 5.57f) * 3f;
			float cx = gap + i * (bw + gap) + bw / 2f;
			float cy = h - 4 - prog * (h - 8);
			float ty = cy + size + 2f;
			if (ty < h - 3)
				g.FillEllipse(trailBrush, cx - size * 0.4f, ty, size * 0.8f, size * 0.8f);
			g.FillEllipse(bubbleBrush, cx - size / 2f, cy - size / 2f, size, size);
		}
	}

	private static void DrawEqTopDrip(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 13;
		GetEqLayout(w, count, out int gap, out int bw);
		int maxH = h - 6, minH = 3;
		using var drop = new SolidBrush(Color.FromArgb(180, c));
		for (int i = 0; i < count; i++)
		{
			float norm = (MathF.Sin(i * 0.72f + t * 0.95f) + 1f) * 0.5f;
			norm = 0.15f + 0.85f * MathF.Pow(norm, 1.4f);
			int bh = minH + (int)(norm * (maxH - minH));
			int x = gap + i * (bw + gap);
			DrawEqBar(g, x, 3, bw, bh, c, false, 0);
			if (norm > 0.75f)
				g.FillEllipse(drop, x + bw / 2f - 1.5f, 3 + bh + 2, 3, 3);
		}
	}

	private static void DrawEqQuakePeaks(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 25;
		GetEqLayout(w, count, out int gap, out int bw);
		float step = MathF.Floor(t * 2f);
		using var b = new SolidBrush(c);
		for (int i = 0; i < count; i++)
		{
			float jitter = EqRand(i * 13.7f + step * 31.3f);
			float norm = 0.12f + 0.88f * MathF.Abs(MathF.Sin(i * 0.53f + t * 1.5f)) * (0.55f + 0.45f * jitter);
			int bh = 3 + (int)(norm * (h - 9));
			float cx = gap + i * (bw + gap) + bw / 2f;
			var pts = new PointF[3]
			{
				new PointF(cx, h - 3 - bh),
				new PointF(cx - bw / 2f - 1f, h - 3),
				new PointF(cx + bw / 2f + 1f, h - 3)
			};
			g.FillPolygon(b, pts);
		}
	}

	private static void DrawEqDiamondTravel(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 9;
		GetEqLayout(w, count, out int gap, out int bw);
		float cy = h / 2f;
		for (int i = 0; i < count; i++)
		{
			float s = 2.5f + 7.5f * (MathF.Sin(i * 0.7f - t * 1.5f) * 0.5f + 0.5f);
			float cx = gap + i * (bw + gap) + bw / 2f;
			var pts = new PointF[4]
			{
				new PointF(cx, cy - s),
				new PointF(cx + s, cy),
				new PointF(cx, cy + s),
				new PointF(cx - s, cy)
			};
			using var b = new SolidBrush(Color.FromArgb((int)(90f + s * 16f), c));
			g.FillPolygon(b, pts);
		}
	}

	private static void DrawEqSnake(Graphics g, int w, int h, Color c, float t)
	{
		const int segs = 14;
		float head = (t * 3.2f) % (w + 60f) - 30f;
		for (int k = 0; k < segs; k++)
		{
			float x = head - k * 9f;
			if (x < -6f || x > w + 6f) continue;
			float y = h / 2f + MathF.Sin(x * 0.32f - t * 1.8f) * 7f;
			float fade = 1f - k / (float)segs;
			using var b = new SolidBrush(Color.FromArgb((int)(235f * fade), c));
			float r = 1.5f + 2.5f * fade;
			g.FillEllipse(b, x - r, y - r, r * 2f, r * 2f);
		}
	}

	private static void DrawEqPluckString(Graphics g, int w, int h, Color c, float t)
	{
		int mid = h / 2;
		const float cycle = 3.5f;
		float p = t / cycle;
		p -= MathF.Floor(p);
		const float pullEnd = 0.14f;
		float amp;
		float osc;
		if (p < pullEnd)
		{
			amp = p / pullEnd;
			osc = 1f;
		}
		else
		{
			amp = MathF.Exp(-(p - pullEnd) * 7f);
			osc = MathF.Cos((p - pullEnd) * cycle * 16f);
		}
		float maxAmp = h / 2f - 5f;
		const int n = 44;
		var pts = new PointF[n];
		const float u0 = 0.38f;
		for (int i = 0; i < n; i++)
		{
			float u = i / (float)(n - 1);
			float shape = 1f - MathF.Abs(u - u0) / MathF.Max(u0, 1f - u0);
			if (shape < 0f) shape = 0f;
			pts[i] = new PointF(u * w, mid - shape * amp * osc * maxAmp);
		}
		using var pen = new Pen(c, 2f);
		g.DrawLines(pen, pts);
		using var cap = new SolidBrush(c);
		g.FillEllipse(cap, 0, mid - 2f, 4f, 4f);
		g.FillEllipse(cap, w - 4f, mid - 2f, 4f, 4f);
		if (p < pullEnd)
		{
			using var pick = new SolidBrush(Color.FromArgb(220, ControlPaint.Light(c, 0.4f)));
			g.FillEllipse(pick, u0 * w - 2f, mid - maxAmp * amp - 3f, 4f, 4f);
		}
	}

	private static void DrawEqDigitalRain(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 17;
		GetEqLayout(w, count, out int gap, out int bw);
		for (int i = 0; i < count; i++)
		{
			float speed = 0.55f + EqRand(i * 3.3f) * 0.75f;
			float prog = EqRand(i * 7.1f) + t * speed;
			prog -= MathF.Floor(prog);
			float x = gap + i * (bw + gap) + bw / 2f - 1f;
			for (int s = 0; s < 3; s++)
			{
				float yHead = prog * (h + 12f) - 6f - s * 5f;
				int alpha = s == 0 ? 230 : (s == 1 ? 110 : 50);
				using var b = new SolidBrush(Color.FromArgb(alpha, c));
				g.FillRectangle(b, x, yHead, 2f, 4f);
			}
		}
	}

	private static void DrawEqContinuousWave(Graphics g, int w, int h, Color c, float t)
	{
		int mid = h / 2;
		float amp = 5f + 3f * MathF.Sin(t * 0.7f);
		var pts = new PointF[w / 4 + 2];
		for (int p = 0; p < pts.Length; p++)
		{
			float x = p * 4f;
			pts[p] = new PointF(x, mid + MathF.Sin(x * 0.11f - t * 2.3f) * amp);
		}
		using var echo = new Pen(Color.FromArgb(70, c), 2f);
		g.TranslateTransform(0f, 2f);
		g.DrawLines(echo, pts);
		g.ResetTransform();
		using var pen = new Pen(c, 2f);
		g.DrawLines(pen, pts);
	}

	private static void DrawEqOrbit(Graphics g, int w, int h, Color c, float t)
	{
		float cx = w / 2f, cy = h / 2f;
		float[,] orbs =
		{
			{ 78f, 7f, 1.4f, 0f },
			{ 52f, 5f, -2.1f, 2.1f },
			{ 92f, 8.5f, 0.9f, 4.2f }
		};
		for (int o = 0; o < 3; o++)
		{
			float rx = orbs[o, 0], ry = orbs[o, 1], sp = orbs[o, 2], ph = orbs[o, 3];
			using (var track = new Pen(Color.FromArgb(36, c), 1f))
				g.DrawEllipse(track, cx - rx, cy - ry, rx * 2f, ry * 2f);
			float a = t * sp + ph;
			float depth = MathF.Sin(a) * 0.5f + 0.5f;
			float x = cx + rx * MathF.Cos(a);
			float y = cy + ry * MathF.Sin(a);
			using var b = new SolidBrush(Color.FromArgb((int)(100f + 140f * depth), c));
			float r = 1.6f + 1.8f * depth;
			g.FillEllipse(b, x - r, y - r, r * 2f, r * 2f);
		}
	}

	private static void DrawEqHFaders(Graphics g, int w, int h, Color c, float t)
	{
		float[] speeds = { 1.1f, 1.7f, 0.8f };
		float[] phases = { 0f, 2.1f, 4.2f };
		int[] lanes = { 5, 12, 19 };
		using var track = new SolidBrush(Color.FromArgb(40, c));
		using var thumb = new SolidBrush(c);
		for (int j = 0; j < 3; j++)
		{
			int ly = lanes[j];
			g.FillRectangle(track, 3, ly, w - 6, 3);
			float u = (t * speeds[j] + phases[j]) % 2f;
			if (u < 0f) u += 2f;
			float pos = u < 1f ? u : 2f - u;
			float tx = 3f + pos * (w - 26f);
			FillRoundRect(g, thumb, (int)tx, ly - 2, 12, 7, 2);
		}
	}

	private static void DrawEqStaircase(Graphics g, int w, int h, Color c, float t)
	{
		const int steps = 8;
		float sw = w / (float)steps;
		int maxStep = h - 8;
		using var stair = new SolidBrush(Color.FromArgb(120, c));
		for (int k = 0; k < steps; k++)
		{
			int sh = 3 + k * maxStep / (steps - 1);
			g.FillRectangle(stair, k * sw, h - 3 - sh, sw - 2, sh);
		}
		float u = t * 0.55f;
		u -= MathF.Floor(u);
		float fu = u * steps;
		int ks = (int)fu;
		float frac = fu - ks;
		int stepH = 3 + ks * maxStep / (steps - 1);
		int nextH = 3 + Math.Min(ks + 1, steps - 1) * maxStep / (steps - 1);
		float curH = stepH + (nextH - stepH) * frac;
		float bx = ks * sw + frac * sw + 2f;
		using var ball = new SolidBrush(c);
		g.FillEllipse(ball, bx, h - 4 - curH - 5f, 5f, 5f);
	}

	private static void DrawEqDualVu(Graphics g, int w, int h, Color c, float t)
	{
		const int segW = 6, segGap = 2;
		int segs = (w - 8) / (segW + segGap);
		float lvlA = 0.15f + 0.85f * (MathF.Sin(t * 1.15f) * 0.5f + 0.5f);
		float lvlB = 0.15f + 0.85f * (MathF.Sin(t * 1.15f + MathF.PI) * 0.5f + 0.5f);
		using var on = new SolidBrush(c);
		using var off = new SolidBrush(Color.FromArgb(36, c));
		for (int j = 0; j < segs; j++)
		{
			int sx = 4 + j * (segW + segGap);
			bool litA = j < (int)(lvlA * segs);
			bool litB = segs - 1 - j < (int)(lvlB * segs);
			FillRoundRect(g, litA ? on : off, sx, 5, segW, 6, 1);
			FillRoundRect(g, litB ? on : off, sx, h - 11, segW, 6, 1);
		}
	}

	private static void DrawEqString(Graphics g, int w, int h, Color c, float t)
	{
		int mid = h / 2;
		int harm = 1 + (int)(t * 0.45f) % 4;
		float amp = (h / 2f - 5f) * (0.55f + 0.45f * MathF.Sin(t * 2.1f));
		const int n = 48;
		var pts = new PointF[n];
		for (int p = 0; p < n; p++)
		{
			float u = p / (float)(n - 1);
			pts[p] = new PointF(u * w, mid + MathF.Sin(harm * MathF.PI * u) * amp * (0.92f + 0.08f * MathF.Sin(t * 13f)));
		}
		using var pen = new Pen(c, 2f);
		g.DrawLines(pen, pts);
		using var cap = new SolidBrush(c);
		g.FillEllipse(cap, 0, mid - 2f, 4f, 4f);
		g.FillEllipse(cap, w - 4f, mid - 2f, 4f, 4f);
	}

	private static void DrawEqConfetti(Graphics g, int w, int h, Color c, float t)
	{
		const int flakes = 20;
		for (int i = 0; i < flakes; i++)
		{
			float speed = 0.45f + EqRand(i * 2.17f) * 0.6f;
			float prog = EqRand(i * 8.9f) + t * speed;
			prog -= MathF.Floor(prog);
			float drift = (EqRand(i * 5.3f) - 0.5f) * 1.6f;
			float x = EqRand(i * 3.77f) * w + prog * drift * w;
			x %= w;
			if (x < 0f) x += w;
			float y = prog * h;
			int alpha = 120 + (int)(130f * EqRand(i * 11.1f));
			using var b = new SolidBrush(Color.FromArgb(alpha, c));
			float sz = 2f + EqRand(i * 6.1f) * 2f;
			g.FillRectangle(b, x, y, sz, sz);
		}
	}

	private static void DrawEqSquareTrain(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 26;
		GetEqLayout(w, count, out int gap, out int bw);
		int maxH = h - 6, minH = 3;
		using var b = new SolidBrush(c);
		for (int i = 0; i < count; i++)
		{
			float ph = i * 0.42f + t * 2.2f;
			bool high = (ph % 2f + 2f) % 2f < 1f;
			int bh = high ? minH + (int)((maxH - minH) * 0.82f) : minH;
			int x = gap + i * (bw + gap);
			g.FillRectangle(b, x, h - 3 - bh, bw, bh);
		}
	}

	private static void DrawEqBreathingDome(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 27;
		GetEqLayout(w, count, out int gap, out int bw);
		float breathe = 0.22f + 0.78f * (MathF.Sin(t * 1.05f) * 0.5f + 0.5f);
		using var b = new SolidBrush(c);
		for (int i = 0; i < count; i++)
		{
			float xc = i / (float)(count - 1) * 2f - 1f;
			float norm = MathF.Exp(-xc * xc * 4.2f) * breathe;
			int bh = 2 + (int)(norm * (h - 8));
			int x = gap + i * (bw + gap);
			g.FillRectangle(b, x, h - 3 - bh, bw, bh);
		}
	}

	private static void DrawEqRadar(Graphics g, int w, int h, Color c, float t)
	{
		int mid = h / 2;
		using (var grid = new Pen(Color.FromArgb(30, c), 1f))
			for (int gx = 6; gx < w; gx += 14)
				g.DrawLine(grid, gx, mid - 4, gx, mid + 4);
		using (var baseLine = new Pen(Color.FromArgb(46, c), 1f))
			g.DrawLine(baseLine, 3, mid, w - 3, mid);
		float span = w + 40f;
		float hx = (t * 4.2f) % span - 20f;
		for (int k = 11; k >= 0; k--)
		{
			float xt = hx - k * 3f;
			if (xt < 3f || xt > w - 3f) continue;
			int alpha = k == 0 ? 240 : (int)(150f * (1f - k / 12f)) + 15;
			int tickH = k == 0 ? 9 : 5;
			using var b = new SolidBrush(Color.FromArgb(alpha, c));
			g.FillRectangle(b, xt - 1f, mid - tickH / 2f, 2f, tickH);
		}
	}

	private static void DrawEqStrobeChecker(Graphics g, int w, int h, Color c, float t)
	{
		int flip = (int)(t * 3f) % 2;
		const int cols = 14;
		float cw = w / (float)cols;
		float ch = (h - 6f) / 2f;
		using var on = new SolidBrush(c);
		using var off = new SolidBrush(Color.FromArgb(34, c));
		for (int row = 0; row < 2; row++)
		{
			for (int k = 0; k < cols; k++)
			{
				bool lit = (k + row + flip) % 2 == 0;
				g.FillRectangle(lit ? on : off, k * cw, 3 + row * ch, cw - 1, ch - 1);
			}
		}
	}

	private static void DrawEqJaws(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 13;
		GetEqLayout(w, count, out int gap, out int bw);
		int half = (h - 8) / 2;
		using var b = new SolidBrush(c);
		for (int i = 0; i < count; i++)
		{
			float norm = 0.15f + 0.85f * (MathF.Sin(i * 0.55f - t * 1.4f) * 0.5f + 0.5f);
			int ah = 2 + (int)(norm * half);
			int x = gap + i * (bw + gap);
			FillRoundRect(g, b, x, 3, bw, ah, 1);
			FillRoundRect(g, b, x, h - 3 - ah, bw, ah, 1);
		}
	}

	private static void DrawEqAnts(Graphics g, int w, int h, Color c, float t)
	{
		const float spacing = 15f;
		float off = (t * 3.4f) % spacing;
		using var b = new SolidBrush(c);
		for (float x = 2f - spacing; x < w + 4f; x += spacing)
		{
			float cx = x + off;
			float hop = MathF.Abs(MathF.Sin(cx * 0.45f - t * 3.4f)) * 4f;
			float cy = h - 5f - hop;
			g.FillEllipse(b, cx, cy, 3.5f, 3.5f);
		}
	}

	private static void DrawEqCoinStacks(Graphics g, int w, int h, Color c, float t)
	{
		const int count = 11;
		GetEqLayout(w, count, out int gap, out int bw);
		using var coin = new SolidBrush(c);
		using var coinHi = new SolidBrush(ControlPaint.Light(c, 0.35f));
		for (int i = 0; i < count; i++)
		{
			float norm = MathF.Sin(i * 0.6f - t * 1.3f) * 0.5f + 0.5f;
			int coinsN = 1 + (int)MathF.Round(norm * 4.4f);
			int x = gap + i * (bw + gap) - 1;
			for (int j = 0; j < coinsN && 3 + j * 3 <= h - 5; j++)
			{
				int cy2 = h - 5 - j * 3;
				g.FillEllipse(j % 2 == 0 ? coin : coinHi, x, cy2 - 2, bw + 2, 4);
			}
		}
	}

	private static void DrawEqChirp(Graphics g, int w, int h, Color c, float t)
	{
		int mid = h / 2;
		float k = 0.10f + 0.07f * MathF.Sin(t * 0.9f);
		const int n = 56;
		var pts = new PointF[n];
		for (int p = 0; p < n; p++)
		{
			float x = p / (float)(n - 1) * w;
			pts[p] = new PointF(x, mid + MathF.Sin(x * k * 6.2832f - t * 2.6f) * 7f);
		}
		using var pen = new Pen(c, 2f);
		g.DrawLines(pen, pts);
	}

	private void SetTimerBackground(Image img, bool fromCache)
	{
		if (!_timerBackgroundCached && timerPanel.BackgroundImage != null)
		{
			timerPanel.BackgroundImage.Dispose();
		}
		_timerBackgroundCached = fromCache;
		timerPanel.BackgroundImage = img;
	}

	private void _fadeTimer_Tick(object? sender, EventArgs e)
	{
		var frames = _fadePhase == 0 ? _fadeOutFrames : _fadeInFrames;
		if (frames == null || _fadeFrameIndex >= frames.Count)
		{
			if (_fadePhase == 0)
			{
				_fadePhase = 1;
				_fadeFrameIndex = 0;
				frames = _fadeInFrames!;
			}
else
				{
					if (_nextCassetteImage != null)
					{
						ReplacePlayerImage(_nextCassetteImage);
						_nextCassetteImage = null;
					}
					ClearFrameList(_fadeOutFrames);
					ClearFrameList(_fadeInFrames);
					RestoreMainDisplayAfterFade();
					_slideTimer?.Stop();
					return;
				}
		}

		picPlayer.Image = frames[_fadeFrameIndex++];
	}

	private static void ClearFrameList(List<Bitmap>? list)
	{
		if (list == null) return;
		for (int i = 0; i < list.Count; i++)
			list[i].Dispose();
		list.Clear();
	}

	private void StartFade(Image newImage)
	{
		if (picPlayer.Image == null)
		{
			ReplacePlayerImage(newImage);
			return;
		}

		ClearFrameList(_fadeOutFrames);
		ClearFrameList(_fadeInFrames);

		_nextCassetteImage = newImage;
		Image oldImg = picPlayer.Image;

		int steps = 5;
		_fadePhase = 0;
		_fadeFrameIndex = 0;
		_fadeOutFrames = new List<Bitmap>(steps + 1);
		_fadeInFrames = new List<Bitmap>(steps);

		float div = 1f / steps;
		try
		{
			for (int i = steps; i >= 0; i--)
				_fadeOutFrames.Add(CreateAlphaCopy(oldImg, i * div));
			for (int i = 1; i <= steps; i++)
				_fadeInFrames.Add(CreateAlphaCopy(newImage, i * div));
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.StartFade", ex);
			ClearFrameList(_fadeOutFrames);
			ClearFrameList(_fadeInFrames);
			_nextCassetteImage = null;
			RestoreMainDisplayAfterFade();
			ReplacePlayerImage(newImage);
			return;
		}

		if (!string.IsNullOrEmpty(_noiseFile))
		{
			_preFadeDisplayImageLocation = picMainDisplay.ImageLocation;
			picMainDisplay.ImageLocation = _noiseFile;
		}

		if (_slideTimer != null)
		{
			_slideTimer.Interval = 24;
			_slideTimer.Start();
		}
	}

	private void RestoreMainDisplayAfterFade()
	{
		string? restore = _preFadeDisplayImageLocation;
		_preFadeDisplayImageLocation = null;
		if (!string.IsNullOrEmpty(restore))
		{
			picMainDisplay.ImageLocation = restore;
		}
	}

	private static Bitmap CreateAlphaCopy(Image img, float alpha)
	{
		var bmp = new Bitmap(PictureBoxWidth, PictureBoxHeight);
		using (var g = Graphics.FromImage(bmp))
		{
			_alphaMatrix.Matrix33 = alpha;
			_alphaAttrs.SetColorMatrix(_alphaMatrix);
			g.DrawImage(img, new Rectangle(0, 0, PictureBoxWidth, PictureBoxHeight),
				0, 0, img.Width, img.Height, GraphicsUnit.Pixel, _alphaAttrs);
		}
		return bmp;
	}

	private void DrawGlow(object? sender, PaintEventArgs e)
	{
		if (!(sender is Label lbl))
		{
			return;
		}
		e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
		string text = lbl.Text;

		// Validación segura del font con fallback a SystemFonts
		Font font = lbl.Font ?? SystemFonts.DefaultFont;
		if (font == null || font.Size <= 0)
		{
			font = SystemFonts.DefaultFont;
		}

		Color glowColor = (_appColor != Color.Transparent) ? _appColor : Color.Cyan;
		for (int i = 1; i <= 2; i++)
		{
			using Brush glowBrush = new SolidBrush(Color.FromArgb(50 / i, glowColor));
			int n = i;
			e.Graphics.DrawString(text, font, glowBrush, new PointF(-n, -n));
			e.Graphics.DrawString(text, font, glowBrush, new PointF(-n, n));
			e.Graphics.DrawString(text, font, glowBrush, new PointF(n, -n));
			e.Graphics.DrawString(text, font, glowBrush, new PointF(n, n));
		}
		using Brush textBrush = new SolidBrush(lbl.ForeColor);
		e.Graphics.DrawString(text, font, textBrush, new PointF(0f, 0f));
	}

	private async Task CycleAni()
	{
		if (_aniFiles.Count != 0)
		{
			if (!string.IsNullOrEmpty(_noiseFile))
			{
				picMainDisplay.ImageLocation = _noiseFile;
				picMainDisplay.Refresh();
				await Task.Delay(800);
			}
			_currentAniIndex = (_currentAniIndex + 1) % _aniFiles.Count;
			picMainDisplay.ImageLocation = _aniFiles[_currentAniIndex];
			picMainDisplay.Refresh();
		}
	}

	private void LoadSpriteSheet(string path, int frameWidth, int frameHeight, int frameCount)
	{
		try
		{
			using Bitmap fullSheet = new Bitmap(path);
			_spriteFrames.Clear();
			int cols = Math.Max(1, fullSheet.Width / frameWidth);
			for (int i = 0; i < frameCount; i++)
			{
				int row = i / cols;
				int col = i % cols;
				Rectangle section = new Rectangle(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
				if (section.Bottom > fullSheet.Height || section.Right > fullSheet.Width)
				{
					_spriteFrames.Clear();
					return;
				}
				Bitmap frame = fullSheet.Clone(section, fullSheet.PixelFormat);
				_spriteFrames.Add(frame);
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.LoadSpriteSheet", ex);
		}
	}
}
