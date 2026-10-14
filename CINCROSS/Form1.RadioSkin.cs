using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CINCROSS;

partial class Form1
{
	private static readonly Color _radioSteelTop = Color.FromArgb(76, 76, 81);
	private static readonly Color _radioSteelBottom = Color.FromArgb(22, 22, 25);
	private static readonly Color _radioBrassLight = Color.FromArgb(224, 186, 108);
	private static readonly Color _radioBrassDark = Color.FromArgb(112, 86, 42);
	private static readonly Color _radioRecess = Color.FromArgb(14, 14, 16);

	private void ApplyRadioSkin()
	{
		cassettesHeaderPanel.Paint += HeaderPlatePaint;
		tasksHeaderPanel.Paint += HeaderPlatePaint;
		toolsHeaderPanel.Paint += HeaderPlatePaint;
		toolsRow.Paint += SectionBodyPaint;
		tasksListPanel.Paint += SectionBodyPaint;
		btnP.Paint += KnobButtonPaint;
		btnS.Paint += KnobButtonPaint;
		btnCloseApp.Paint += ClosePlatePaint;
		pnlProgressBg.Paint += ScaleSlotPaint;

		cassettesHeaderPanel.Invalidate();
		tasksHeaderPanel.Invalidate();
		toolsHeaderPanel.Invalidate();
		toolsRow.Invalidate();
		tasksListPanel.Invalidate();
		btnP.Invalidate();
		btnS.Invalidate();
		btnCloseApp.Invalidate();
		pnlProgressBg.Invalidate();
	}

	private void SectionBodyPaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Control c) return;
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = c.ClientRectangle;
		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);
		if (_appColor != Color.Transparent)
		{
			using var tint = new SolidBrush(Color.FromArgb(102, _appColor));
			g.FillRectangle(tint, r);
		}
		if (ReferenceEquals(sender, tasksListPanel))
		{
			using var wash = new SolidBrush(TimerWash);
			g.FillRectangle(wash, r);
		}
	}

	private void DrawFooterSkin(Graphics g)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = playerFooterPanel.ClientRectangle;
		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);
		Rectangle grille = pnlCassetteContainer.Bounds;
		grille.Inflate(-4, -6);
		if (grille.Width > 0 && grille.Height > 0)
			DrawGrille(g, grille);
		using (var trim = new Pen(Color.FromArgb(150, 196, 164, 92), 1f))
			g.DrawLine(trim, 0, 91, r.Width, 91);
		using (var top = new Pen(Color.FromArgb(80, 255, 255, 255), 1f))
			g.DrawLine(top, 0, 0, r.Width, 0);
		DrawCornerScrews(g, r);
	}

	private void HeaderPlatePaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Control c) return;
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using (var brush = MetalBrush(new RectangleF(0, 0, c.Width, c.Height)))
			e.Graphics.FillRectangle(brush, c.ClientRectangle);
		DrawBrushed(e.Graphics, c.ClientRectangle);
		using (var top = new Pen(Color.FromArgb(80, 255, 255, 255), 1f))
			e.Graphics.DrawLine(top, 0, 0, c.Width, 0);
		using (var brass = new Pen(Color.FromArgb(150, 196, 164, 92), 1f))
			e.Graphics.DrawLine(brass, 0, c.Height - 1, c.Width, c.Height - 1);
		DrawCornerScrews(e.Graphics, c.ClientRectangle);
	}

	private void KnobButtonPaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Button b) return;
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		float cx = b.Width / 2f;
		float cy = b.Height / 2f;
		float r = Math.Min(b.Width, b.Height) / 2f - 3.5f;
		using (var socket = new SolidBrush(Color.FromArgb(12, 12, 14)))
			e.Graphics.FillEllipse(socket, cx - r - 2f, cy - r - 2f, r * 2f + 4f, r * 2f + 4f);
		using (var rim = new SolidBrush(Color.FromArgb(170, _radioBrassDark)))
			e.Graphics.FillEllipse(rim, cx - r - 1f, cy - r - 1f, r * 2f + 2f, r * 2f + 2f);
		using (var face = MetalBrush(new RectangleF(cx - r, cy - r, r * 2f, r * 2f)))
			e.Graphics.FillEllipse(face, cx - r, cy - r, r * 2f, r * 2f);
		using (var hi = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
			e.Graphics.FillEllipse(hi, cx - r, cy - r, r * 2f, r * 2f * 0.45f);
		using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
		using (var brush = new SolidBrush(Color.White))
			e.Graphics.DrawString(b.Text, b.Font, brush, new RectangleF(0, 0, b.Width, b.Height), fmt);
	}

	private void ClosePlatePaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Button b) return;
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using (var brush = MetalBrush(new RectangleF(0, 0, b.Width, b.Height)))
			e.Graphics.FillRectangle(brush, b.ClientRectangle);
		DrawBrushed(e.Graphics, b.ClientRectangle);
		using (var brass = new Pen(Color.FromArgb(150, 196, 164, 92), 1f))
			e.Graphics.DrawRectangle(brass, 1, 1, b.Width - 3, b.Height - 3);
		DrawCornerScrews(e.Graphics, b.ClientRectangle);
		using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
		using (var brush = new SolidBrush(Color.White))
			e.Graphics.DrawString(b.Text, b.Font, brush, new RectangleF(0, 0, b.Width, b.Height), fmt);
	}

	private void VolumeKnobPaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Button b) return;
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		bool selected = b.Tag is bool sel && sel;
		float cx = b.Width / 2f;
		float cy = b.Height / 2f;
		float r = Math.Min(b.Width, b.Height) / 2f - 1f;
		using (var socket = new SolidBrush(Color.FromArgb(12, 12, 14)))
			e.Graphics.FillEllipse(socket, cx - r - 1.5f, cy - r - 1.5f, r * 2f + 3f, r * 2f + 3f);
		using (var rim = new SolidBrush(selected ? Color.FromArgb(230, 220, 200, 150) : Color.FromArgb(170, _radioBrassDark)))
			e.Graphics.FillEllipse(rim, cx - r, cy - r, r * 2f, r * 2f);
		RectangleF faceRect = new RectangleF(cx - r + 2.5f, cy - r + 2.5f, r * 2f - 5f, r * 2f - 5f);
		using (var face = new LinearGradientBrush(faceRect, Color.FromArgb(30, 30, 36), Color.FromArgb(12, 12, 15), LinearGradientMode.Vertical))
			e.Graphics.FillEllipse(face, faceRect);
		RectangleF domeRect = new RectangleF(faceRect.X, faceRect.Y, faceRect.Width, faceRect.Height * 0.55f);
		using (var dome = new LinearGradientBrush(domeRect, Color.FromArgb(120, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
			e.Graphics.FillEllipse(dome, domeRect);
		DrawKnobDots(e.Graphics, cx, cy, r - 1.25f, selected);
		if (selected)
		{
			using (var ind = new Pen(Color.White, 2f))
			{
				ind.StartCap = LineCap.Round;
				ind.EndCap = LineCap.Round;
				e.Graphics.DrawLine(ind, cx - r * 0.3f, cy + r - r * 0.25f, cx + r * 0.3f, cy + r - r * 0.25f);
			}
		}
		using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
		using (var brush = new SolidBrush(selected ? Color.White : Color.FromArgb(200, 200, 200)))
			e.Graphics.DrawString(b.Text, b.Font, brush, new RectangleF(0, 0, b.Width, b.Height), fmt);
	}

	private void AbcGrooveBackPaint(Graphics g, int x, int y, int w, int h, int gap)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		RectangleF slot = new RectangleF(x - 8, y - 5, w + 10, h * 3 + gap * 2 + 10);
		using (var path = RoundedPath(slot, 12f))
		{
			using (var recess = new SolidBrush(_radioRecess))
				g.FillPath(recess, path);
			using (var brass = new Pen(Color.FromArgb(140, _radioBrassDark), 1.3f))
				g.DrawPath(brass, path);
			using (var shadow = new Pen(Color.FromArgb(70, 0, 0, 0), 1.2f))
				g.DrawLine(shadow, slot.X + 1.5f, slot.Y + 2, slot.X + 1.5f, slot.Bottom - 2);
			using (var hi = new Pen(Color.FromArgb(55, 255, 255, 255), 1f))
				g.DrawLine(hi, slot.X + 4.5f, slot.Y + 4, slot.X + 4.5f, slot.Bottom - 4);
		}
		using (var tick = new Pen(Color.FromArgb(170, _radioBrassLight), 1.3f))
		{
			for (int i = 0; i < 3; i++)
			{
				float my = y + i * (h + gap) + h / 2f;
				g.DrawLine(tick, slot.X - 3, my - 5, slot.X + 7, my - 5);
				g.DrawLine(tick, slot.X - 3, my + 5, slot.X + 7, my + 5);
			}
		}
	}

	private void AbcGroovePaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Button b)
		{
			return;
		}
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
		bool selected = b.Tag is bool sel && sel;
		RectangleF rect = new RectangleF(
			selected ? 5 : 6,
			selected ? 5 : 4,
			b.Width - (selected ? 10 : 12),
			b.Height - (selected ? 10 : 8));
		using (var path = RoundedPath(rect, 7f))
		{
			using (var brush = MetalBrush(rect))
				e.Graphics.FillPath(brush, path);
			DrawBrushed(e.Graphics, Rectangle.Truncate(rect));
			using (var brass = new Pen(selected ? Color.FromArgb(235, 228, 205, 150) : Color.FromArgb(155, _radioBrassDark), selected ? 1.6f : 1.1f))
				e.Graphics.DrawPath(brass, path);
			if (selected)
			{
				using (var hi = new Pen(Color.FromArgb(130, 255, 255, 255), 1.2f))
					e.Graphics.DrawLine(hi, rect.X + 3, rect.Y + 1, rect.Right - 3, rect.Y + 1);
			}
		}
		using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
		{
			using (var outline = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
			{
				e.Graphics.DrawString(b.Text, b.Font, outline, new RectangleF(rect.X + 0.8f, rect.Y + 1f, rect.Width, rect.Height), fmt);
				e.Graphics.DrawString(b.Text, b.Font, outline, new RectangleF(rect.X - 0.8f, rect.Y - 1f, rect.Width, rect.Height), fmt);
			}
			using (var ink = new SolidBrush(selected ? Color.White : Color.FromArgb(185, 205, 205, 210)))
				e.Graphics.DrawString(b.Text, b.Font, ink, rect, fmt);
		}
		if (selected)
		{
			using (var needle = new Pen(Color.FromArgb(235, 228, 205, 150), 2f))
				e.Graphics.DrawLine(needle, rect.X - 6, rect.Y + rect.Height / 2f, rect.X - 1, rect.Y + rect.Height / 2f);
		}
	}

	private static void DrawKnobDots(Graphics g, float cx, float cy, float rr, bool selected)
	{
		int count = Math.Max(16, (int)Math.Round(rr * 2f));
		float dotR = rr / count * 1.7f;
		using (var light = new SolidBrush(selected ? Color.FromArgb(230, 220, 200, 150) : _radioBrassLight))
		using (var dark = new SolidBrush(selected ? Color.FromArgb(90, 20, 20, 24) : _radioRecess))
		{
			for (int i = 0; i < count; i++)
			{
				float ang = i * (2f * (float)Math.PI / count);
				float x = cx + (float)Math.Cos(ang) * rr;
				float y = cy + (float)Math.Sin(ang) * rr;
				g.FillEllipse(i % 2 == 0 ? light : dark, x - dotR, y - dotR, dotR * 2f, dotR * 2f);
			}
		}
	}

	private void JackSocketPaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Control c) return;
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Color border = c.Tag is Color col ? col : Color.White;
		float cx = c.Width / 2f;
		float cy = c.Height / 2f;
		float r = Math.Min(c.Width, c.Height) / 2f - 1.5f;
		using (var recess = new SolidBrush(_radioRecess))
			e.Graphics.FillEllipse(recess, cx - r, cy - r, r * 2f, r * 2f);
		using (var pen = new Pen(border, 2f))
			e.Graphics.DrawEllipse(pen, cx - r + 0.5f, cy - r + 0.5f, r * 2f - 1f, r * 2f - 1f);
		using (var pen2 = new Pen(border, 1.5f))
			e.Graphics.DrawEllipse(pen2, cx - r + 3.5f, cy - r + 3.5f, r * 2f - 7f, r * 2f - 7f);
		float hr = 2.8f;
		using (var hole = new SolidBrush(Color.FromArgb(3, 3, 4)))
			e.Graphics.FillEllipse(hole, cx - hr, cy - hr, hr * 2f, hr * 2f);
	}

	private void CassetteGlassPaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Control c) return;
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = c.ClientRectangle;
		using (var sheen = new LinearGradientBrush(r, Color.FromArgb(32, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
			g.FillRectangle(sheen, r);
		PointF[] band =
		[
			new PointF(r.Left + 10f, r.Top - 10f),
			new PointF(r.Left + 90f, r.Top - 10f),
			new PointF(r.Left + 120f, r.Bottom + 10f),
			new PointF(r.Left + 40f, r.Bottom + 10f)
		];
		using (var bandBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
			g.FillPolygon(bandBrush, band);
		using (var edge = new Pen(Color.FromArgb(80, 255, 255, 255), 1f))
			g.DrawLine(edge, band[0], band[1]);
	}

	private void ScaleSlotPaint(object? sender, PaintEventArgs e)
	{
		if (sender is not Control c) return;
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using (var recess = new SolidBrush(_radioRecess))
			e.Graphics.FillRectangle(recess, c.ClientRectangle);
		using (var top = new Pen(Color.FromArgb(60, 0, 0, 0), 1f))
			e.Graphics.DrawLine(top, 0, 0, c.Width, 0);
		using (var brass = new Pen(Color.FromArgb(150, 196, 164, 92), 1f))
		{
			e.Graphics.DrawLine(brass, 0, 1, c.Width, 1);
			e.Graphics.DrawLine(brass, 0, c.Height - 2, c.Width, c.Height - 2);
		}
		using (var tick = new Pen(Color.FromArgb(110, 170, 145, 85), 1f))
		{
			for (int i = 0; i <= 10; i++)
			{
				int x = i * c.Width / 10;
				int len = i % 5 == 0 ? 6 : 3;
				e.Graphics.DrawLine(tick, x, c.Height - 3 - len, x, c.Height - 3);
			}
		}
	}

	private static LinearGradientBrush MetalBrush(RectangleF r)
	{
		return new LinearGradientBrush(r, _radioSteelTop, _radioSteelBottom, LinearGradientMode.Vertical);
	}

	private static void DrawBrushed(Graphics g, Rectangle r)
	{
		using (var pen = new Pen(Color.FromArgb(18, 255, 255, 255), 1f))
		{
			for (int y = r.Y + 3; y < r.Bottom; y += 4)
				g.DrawLine(pen, r.X, y, r.Right, y);
		}
	}

	private static void DrawGrille(Graphics g, Rectangle r)
	{
		using (var hole = new SolidBrush(Color.FromArgb(90, 8, 8, 10)))
		using (var rim = new Pen(Color.FromArgb(60, 214, 200, 170), 1f))
		{
			for (int y = r.Y + 4; y < r.Bottom; y += 6)
			{
				for (int x = r.X + 4; x < r.Right; x += 6)
				{
					g.FillEllipse(hole, x - 2.2f, y - 2.2f, 4.4f, 4.4f);
					g.DrawEllipse(rim, x - 2.2f, y - 2.2f, 4.4f, 4.4f);
				}
			}
		}
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

	private static void DrawCornerScrews(Graphics g, Rectangle r, bool tl = true, bool tr = true, bool bl = true, bool br = true)
	{
		float inset = 4f;
		if (tl) DrawScrew(g, r.X + inset, r.Y + inset);
		if (tr) DrawScrew(g, r.Right - inset, r.Y + inset);
		if (bl) DrawScrew(g, r.X + inset, r.Bottom - inset);
		if (br) DrawScrew(g, r.Right - inset, r.Bottom - inset);
	}
}
