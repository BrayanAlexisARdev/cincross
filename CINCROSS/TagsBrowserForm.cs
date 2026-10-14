using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CINCROSS;

/// <summary>
/// Ventana lateral (FULL LIST). Lee los TAGS de cada cassette del CASS_master.txt
/// y los agrupa en categorías ordenadas, con búsqueda de texto y navegación directa.
/// Comparte la estética "radio/steel" de la aplicación principal.
/// </summary>
public class TagsBrowserForm : Form
{
	private const int FormWidth = 259;
	private const int CornerRadius = 12;
	private const string NoTagCategory = "SIN CATEGOR\u00CDA";

	private static readonly Color SteelTop = Color.FromArgb(76, 76, 81);
	private static readonly Color SteelBottom = Color.FromArgb(22, 22, 25);
	private static readonly Color BrassLight = Color.FromArgb(224, 186, 108);
	private static readonly Color BrassDark = Color.FromArgb(112, 86, 42);
	private static readonly Color BrassLine = Color.FromArgb(150, 196, 164, 92);
	private static readonly Color Recess = Color.FromArgb(14, 14, 16);
	private static readonly Color ScreenBg = Color.FromArgb(18, 18, 21);
	private static readonly Color TextMain = Color.FromArgb(226, 226, 230);
	private static readonly Color TextDim = Color.FromArgb(150, 150, 156);
	private static readonly Color CategoryColor = Color.FromArgb(255, 205, 84);
	private static readonly Color Accent = Color.FromArgb(120, 170, 255);

	private readonly CassetteData[] _cassettes;
	private readonly Action<int> _onOpenCassette;

	private readonly Panel _header;
	private readonly Panel _searchPanel;
	private readonly Panel _treeHost;
	private readonly Panel _bottomPanel;
	private readonly TextBox _txtSearch;
	private readonly TreeView _tree;
	private readonly Label _lblStatus;
	private readonly Font _boldFont = new Font("Segoe UI", 9f, FontStyle.Bold);
	private readonly Font _headerFont = new Font("Segoe UI", 11f, FontStyle.Bold);

	[DllImport("Gdi32.dll")]
	private static extern nint CreateRoundRectRgn(int left, int top, int right, int bottom, int w, int h);

	[DllImport("Gdi32.dll")]
	private static extern bool DeleteObject(nint hObject);

	public TagsBrowserForm(CassetteData[] cassettes, Action<int> onOpenCassette, int targetHeight)
	{
		_cassettes = cassettes ?? Array.Empty<CassetteData>();
		_onOpenCassette = onOpenCassette;

		Text = "FULL LIST";
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.Manual;
		Size = new Size(FormWidth, Math.Max(360, targetHeight));
		MinimumSize = new Size(FormWidth, 360);
		BackColor = SteelBottom;
		ShowInTaskbar = false;
		KeyPreview = true;
		DoubleBuffered = true;

		_header = new Panel { Dock = DockStyle.Top, Height = 44, Name = "header" };
		_header.Paint += HeaderPaint;

		_searchPanel = new Panel { Dock = DockStyle.Top, Height = 46, Name = "searchPanel" };
		_searchPanel.Paint += SearchPanelPaint;

		_txtSearch = new TextBox
		{
			BorderStyle = BorderStyle.None,
			BackColor = ScreenBg,
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 9f),
			PlaceholderText = "Buscar...",
			Location = new Point(11, 13),
			Size = new Size(FormWidth - 22, 20)
		};
		_txtSearch.TextChanged += (_, _) => RebuildTree();
		_searchPanel.Controls.Add(_txtSearch);

		_treeHost = new Panel { Dock = DockStyle.Fill, Name = "treeHost", Padding = new Padding(6, 5, 6, 5) };
		_treeHost.Paint += TreeHostPaint;

		_tree = new TreeView
		{
			Dock = DockStyle.Fill,
			BorderStyle = BorderStyle.None,
			BackColor = ScreenBg,
			ForeColor = TextMain,
			LineColor = Color.FromArgb(70, 70, 74),
			Font = new Font("Segoe UI", 9f),
			FullRowSelect = true,
			HideSelection = false,
			ShowLines = false,
			ShowPlusMinus = true,
			ShowRootLines = false,
			ItemHeight = 24,
			Indent = 16,
			DrawMode = TreeViewDrawMode.OwnerDrawText
		};
		_tree.DrawNode += TreeDrawNode;
		_tree.NodeMouseDoubleClick += (_, e) => OpenNode(e.Node);
		_tree.KeyDown += (_, e) =>
		{
			if (e.KeyCode == Keys.Enter)
			{
				OpenNode(_tree.SelectedNode);
				e.SuppressKeyPress = true;
			}
		};
		_treeHost.Controls.Add(_tree);

		_bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 34, Name = "bottomPanel" };
		_bottomPanel.Paint += BottomPanelPaint;

		_lblStatus = new Label
		{
			Dock = DockStyle.Left,
			Width = FormWidth - 84,
			ForeColor = TextDim,
			Font = new Font("Segoe UI", 7f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Padding = new Padding(10, 0, 0, 0),
			BackColor = Color.Transparent,
			Text = "Doble clic para abrir"
		};

		Button btnClose = new Button
		{
			Text = "CERRAR",
			Dock = DockStyle.Right,
			Width = 78,
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0 },
			BackColor = Color.Transparent,
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 7f, FontStyle.Bold),
			UseVisualStyleBackColor = false,
			Cursor = Cursors.Hand
		};
		btnClose.Paint += BrassButtonPaint;
		btnClose.Click += (_, _) => Close();

		_bottomPanel.Controls.Add(_lblStatus);
		_bottomPanel.Controls.Add(btnClose);

		Controls.Add(_bottomPanel);
		Controls.Add(_treeHost);
		Controls.Add(_searchPanel);
		Controls.Add(_header);

		KeyDown += (_, e) =>
		{
			if (e.KeyCode == Keys.Escape) Close();
		};

		RebuildTree();
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
		using (var glow = new SolidBrush(Color.FromArgb(70, CategoryColor)))
		{
			g.DrawString("FULL LIST", _headerFont, glow, new RectangleF(1, 1, r.Width, r.Height), fmt);
			g.DrawString("FULL LIST", _headerFont, glow, new RectangleF(-1, -1, r.Width, r.Height), fmt);
		}
		using (var ink = new SolidBrush(Color.FromArgb(238, 238, 242)))
			g.DrawString("FULL LIST", _headerFont, ink, textRect, fmt);

		DrawCornerScrews(g, r);
	}

	private void SearchPanelPaint(object? sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = _searchPanel.ClientRectangle;

		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);

		Rectangle slot = _txtSearch.Bounds;
		slot.Inflate(4, 3);
		using (var rec = new SolidBrush(Recess))
			g.FillRectangle(rec, slot);
		using (var brass = new Pen(Color.FromArgb(150, BrassDark), 1.2f))
			g.DrawRectangle(brass, slot.X, slot.Y, slot.Width - 1, slot.Height - 1);
		using (var hi = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
			g.DrawLine(hi, slot.X + 1, slot.Bottom - 2, slot.Right - 1, slot.Bottom - 2);
	}

	private void TreeHostPaint(object? sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = _treeHost.ClientRectangle;

		using (var brush = MetalBrush(new RectangleF(0, 0, r.Width, r.Height)))
			g.FillRectangle(brush, r);
		DrawBrushed(g, r);

		Rectangle screen = _tree.Bounds;
		screen.Inflate(2, 2);
		using (var rec = new SolidBrush(Recess))
			g.FillRectangle(rec, screen);
		using (var brass = new Pen(Color.FromArgb(140, BrassDark), 1.2f))
			g.DrawRectangle(brass, screen.X, screen.Y, screen.Width - 1, screen.Height - 1);
		using (var hi = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
			g.DrawLine(hi, screen.X + 1, screen.Bottom - 2, screen.Right - 1, screen.Bottom - 2);
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

	private void TreeDrawNode(object? sender, DrawTreeNodeEventArgs e)
	{
		if (e.Node == null) return;
		bool selected = (e.State & TreeNodeStates.Selected) != 0;
		Color color = selected
			? Color.White
			: (e.Node.Tag is string ? CategoryColor : TextMain);
		Font font = e.Node.NodeFont ?? _tree.Font;

		TextRenderer.DrawText(
			e.Graphics,
			e.Node.Text,
			font,
			e.Bounds,
			color,
			TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
			TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
	}

	private void RebuildTree()
	{
		_tree.BeginUpdate();
		_tree.Nodes.Clear();

		string filter = _txtSearch.Text.Trim();
		Dictionary<string, List<int>> categories = BuildCategories();

		List<string> names = categories.Keys.ToList();
		names.Sort(StringComparer.OrdinalIgnoreCase);

		int shownCategories = 0;
		int shownCassettes = 0;

		foreach (string name in names)
		{
			List<int> indices = categories[name];
			bool categoryMatches = filter.Length == 0 ||
				name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

			List<int> visible = categoryMatches
				? indices
				: indices.Where(i => _cassettes[i].Titulo
					.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

			if (visible.Count == 0) continue;

			TreeNode root = new TreeNode($"{name}   ({visible.Count})")
			{
				Tag = name,
				NodeFont = _boldFont
			};

			foreach (int idx in visible)
			{
				string title = _cassettes[idx].Titulo.Trim();
				root.Nodes.Add(new TreeNode($"{idx + 1:00} \u00B7 {title}")
				{
					Tag = idx
				});
			}

			_tree.Nodes.Add(root);
			shownCategories++;
			shownCassettes += visible.Count;
		}

		_tree.EndUpdate();
		foreach (TreeNode root in _tree.Nodes) root.Expand();

		_lblStatus.ForeColor = TextDim;
		_lblStatus.Text = $"{shownCategories} cat \u00B7 {shownCassettes} cassettes";
	}

	private Dictionary<string, List<int>> BuildCategories()
	{
		Dictionary<string, List<int>> map = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < _cassettes.Length; i++)
		{
			string[] tags = (_cassettes[i].Tags ?? "")
				.Split(',', StringSplitOptions.RemoveEmptyEntries);

			bool any = false;
			foreach (string raw in tags)
			{
				string tag = raw.Trim();
				if (tag.Length == 0) continue;
				AddToCategory(map, tag, i);
				any = true;
			}
			if (!any) AddToCategory(map, NoTagCategory, i);
		}
		return map;
	}

	private static void AddToCategory(Dictionary<string, List<int>> map, string tag, int index)
	{
		if (!map.TryGetValue(tag, out List<int>? list))
		{
			list = new List<int>();
			map[tag] = list;
		}
		list.Add(index);
	}

	private void OpenNode(TreeNode? node)
	{
		if (node?.Tag is int index && index >= 0 && index < _cassettes.Length)
		{
			_onOpenCassette?.Invoke(index);
			_lblStatus.Text = $"CASSETTE {index + 1:00} \u00B7 {_cassettes[index].Titulo.Trim()}";
			_lblStatus.ForeColor = Accent;
		}
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
			_boldFont.Dispose();
			_headerFont.Dispose();
		}
		base.Dispose(disposing);
	}
}
