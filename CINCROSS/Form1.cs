using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CINCROSS;

public partial class Form1 : Form
{
	protected override CreateParams CreateParams
	{
		get
		{
			const int CS_DROPSHADOW = 0x20000;
			CreateParams cp = base.CreateParams;
			cp.ClassStyle |= CS_DROPSHADOW;
			return cp;
		}
	}

	private IContainer components = null!;

	private static readonly Color TimerWash = Color.FromArgb(128, Color.Black);

	private BufferedPanel timerPanel = null!;

	private BufferedLabel lblHours = null!;

	private BufferedLabel lblMinutes = null!;

	private BufferedLabel lblSeconds = null!;

	private BufferedPanel pnlTopButtons = null!;

	private Button btnP = null!;

	private Button btnS = null!;

	private Panel pnlTimerControls = null!;

	private Button btnTheme = null!;

	private Button btnImage = null!;

	private Button btnColor = null!;

	private BufferedPanel cassettesHeaderPanel = null!;

	private Label lblCassettes = null!;
	private Label lblCassetteCount = null!;
	private TextBox txtCassetteNum = null!;
	private Label lblCassetteTotal = null!;
	private Button btnCassetteList = null!;
	private Button btnFavList = null!;
	private Button btnAddFav = null!;
	private Button btnLearn = null!;
	private Button btnInfo = null!;
	private Panel pnlFullListRow = null!;

	private BufferedPanel tasksHeaderPanel = null!;

	private Label lblTasks = null!;

	private BufferedPanel toolsHeaderPanel = null!;

	private Label lblTools = null!;

	private Panel toolsRow = null!;

	private BufferedPanel playerFooterPanel = null!;

	private Label lblM3uTitle = null!;

	private Label lblMetadata = null!;

	private Label lblExtraMetadata = null!;

	private PictureBox picPlayer = null!;

	private PictureBox picPlayerNext = null!;

	private BufferedPanel pnlCassetteContainer = null!;

	private BufferedPanel pnlEqualizer = null!;

	private PictureBox picMainDisplay = null!;

	private FlowLayoutPanel tasksListPanel = null!;

	private Label lblTaskInfo = null!;

	private Label lblTaskRemaining = null!;

	private Label lblTimeSelector = null!;

	private bool _presetBlockMsg;

	private Button? _hourPresetBtn;

	private Button? _minPresetBtn;

	private Panel pnlProgressBg = null!;

	private Panel pnlProgressFill = null!;

	private const int NodeStandardSize = 5;

	private const double NodeMaxScale = 2.5;

	private const int NodeMaxBaseSize = (int)(NodeStandardSize * NodeMaxScale);

	private PictureBox picOverlay = null!;

	private Button btnPlayPlayer = null!;

	private Button btnStopPlayer = null!;

	private Panel pnlVolume = null!;

	private Panel pnlVolumeLine = null!;

	private Panel pnlVolumeThumb = null!;
private Button btnVolLow = null!;

	private Button btnVolMid = null!;

	private Button btnVolMax = null!;

	private Button btnAbcA = null!;

	private Button btnAbcB = null!;

	private Button btnAbcC = null!;
	private BufferedPanel pnlGrip = null!;

	private Button btnCloseApp = null!;
	private PictureBox picCincross = null!;

	private double[] _nodeIntensities = Array.Empty<double>();
	private int[] _nodeBaseSizes = Array.Empty<int>();
	private float[] _nodeCenterX = Array.Empty<float>();
	private int _nodeCount;

	private BufferedPanel? pnlStationTitle;

	[DllImport("Gdi32.dll")]
	private static extern nint CreateRoundRectRgn(int nLeftRect, int nTopRect, int RightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

	private const int FormCornerRadius = 12;

	private void ApplyRoundedFormRegion()
	{
		Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, base.Width, base.Height, FormCornerRadius, FormCornerRadius));
	}

	private void ApplyBrandIcon()
	{
		try
		{
			using Stream? stream = typeof(Form1).Assembly.GetManifestResourceStream("CINCROSS.app.ico");
			if (stream != null)
			{
				this.Icon = new System.Drawing.Icon(stream);
				return;
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.ApplyBrandIcon", ex);
		}

		try
		{
			this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.ApplyBrandIcon.Fallback", ex);
		}
	}

	public Form1()
	{
		InitializeComponent();
		ApplyBrandIcon();
		DoubleBuffered = true;
		SetupLighthouse();
		tasksListPanel.HorizontalScroll.Enabled = false;
		base.FormBorderStyle = FormBorderStyle.None;
		ApplyRoundedFormRegion();
		WireSurfacePaint();
		LoadCustomFont();
		base.Load += Form1_Load;
		WireHeaderButtons();
		WireTransportButtons();
		WireVolumeInteraction();
		WireAnimationTimers();
		ApplyInitialBackColors();
		WireTimerLabels();
		WireFormDrag();
		lblM3uTitle.Visible = true;
		lblMetadata.Visible = true;
		lblExtraMetadata.Visible = true;
		pnlGrip.Height = 20;
		timerPanel.Paint += TimerPanelOverlayPaint;
		UpdateTimerDisplay();
	}

	private void TimerPanelOverlayPaint(object? s, PaintEventArgs e)
	{
		PaintAppColorOverlay(e.Graphics, timerPanel.Width, timerPanel.Height);
	}

	private void PaintAppColorOverlay(Graphics g, int width, int height)
	{
		if (_appColor == Color.Transparent) return;
		using Brush brush = new SolidBrush(Color.FromArgb(102, _appColor));
		g.FillRectangle(brush, 0, 0, width, height);
	}

	private void TasksListPanelPaint(object? s, PaintEventArgs e)
	{
		using Brush wash = new SolidBrush(TimerWash);
		e.Graphics.FillRectangle(wash, e.ClipRectangle);
	}

	private void PlayerFooterPanelPaint(object? s, PaintEventArgs e)
	{
		DrawFooterSkin(e.Graphics);
		PaintAppColorOverlay(e.Graphics, playerFooterPanel.Width, playerFooterPanel.Height);
	}

	private void WireSurfacePaint()
	{
		tasksListPanel.Paint += TasksListPanelPaint;
		playerFooterPanel.Paint += PlayerFooterPanelPaint;
	}

	private void WireHeaderButtons()
	{
		btnS.Click += btnS_Click;
		btnP.Click += btnP_Click;
		btnLearn.Click += btnLearn_Click;
		btnColor.Click += delegate { ShowColorMenu(); };
		btnImage.Click += delegate { _ = CycleAni(); };
		btnTheme.Click += delegate { CycleThemeImage(); };
		txtCassetteNum.KeyDown += txtCassetteNum_KeyDown;
		txtCassetteNum.Leave += txtCassetteNum_Leave;
	}

	private void WireTransportButtons()
	{
		btnPlayPlayer.Click += async delegate { await ResumePlaybackAsync(); };
		btnStopPlayer.Click += delegate { StopPlayback(); };
	}

	private async Task ResumePlaybackAsync()
	{
		if (_isHlsStream && _hlsPlayer != null)
		{
			if (!_hlsPlayer.IsPlaying && _lastHlsUrl != null)
				await _hlsPlayer.PlayAsync(_lastHlsUrl);
		}
		else
		{
			_wmp?.controls.play();
		}
		_isPlaying = true;
	}

	private void StopPlayback()
	{
		StopM3u();
		_isPlaying = false;
	}

	private void WireVolumeInteraction()
	{
		pnlVolume.MouseDown += delegate(object? s, MouseEventArgs e)
		{
			_isDraggingVolume = true;
			UpdateVolumeFromMouse(e.X);
		};
		pnlVolume.MouseMove += delegate(object? s, MouseEventArgs e)
		{
			if (_isDraggingVolume)
			{
				UpdateVolumeFromMouse(e.X);
			}
		};
		pnlVolume.MouseUp += delegate { _isDraggingVolume = false; };
		pnlVolumeThumb.MouseDown += delegate { _isDraggingVolume = true; };
		pnlVolumeThumb.MouseMove += delegate(object? s, MouseEventArgs e)
		{
			if (_isDraggingVolume)
			{
				UpdateVolumeFromMouse(pnlVolumeThumb.Left + e.X + pnlVolumeLine.Left);
			}
		};
		pnlVolumeThumb.MouseUp += delegate { _isDraggingVolume = false; };
		pnlVolumeThumb.Paint += delegate(object? s, PaintEventArgs e)
		{
			using Brush brush = new SolidBrush(Color.White);
			int w = pnlVolumeThumb.Width;
			int h = pnlVolumeThumb.Height;
			Point[] triangle = new Point[3]
			{
				new Point(w / 2, 0),
				new Point(0, h),
				new Point(w, h)
			};
			e.Graphics.FillPolygon(brush, triangle);
		};
		btnVolLow.Click += delegate { SetVolumePreset(3); };
		btnVolMid.Click += delegate { SetVolumePreset(9); };
		btnVolMax.Click += delegate { SetVolumePreset(15); };
	}

	private void WireAnimationTimers()
	{
		_slideTimer = new Timer
		{
			Interval = 125
		};
		_slideTimer.Tick += _fadeTimer_Tick;
		_eqTimer = new Timer
		{
			Interval = 100
		};
		_eqTimer.Tick += delegate
		{
			if (!_isPlaying)
			{
				if (_eqIdlePainted) return;
				_eqIdlePainted = true;
			}
			else
			{
				_eqAnimationOffset += 0.15f;
				_eqIdlePainted = false;
			}
			pnlEqualizer.Invalidate();
		};
		pnlEqualizer.Paint += pnlEqualizer_Paint;
	}

	private void ApplyInitialBackColors()
	{
		Color clear = Color.FromArgb(0, 0, 0, 0);
		btnColor.BackColor = clear;
		btnImage.BackColor = clear;
		btnTheme.BackColor = clear;
		btnP.BackColor = clear;
		btnS.BackColor = clear;
		btnVolLow.BackColor = clear;
		btnVolMid.BackColor = clear;
		btnVolMax.BackColor = clear;
		picPlayer.BackColor = Color.Transparent;
		picPlayerNext.BackColor = Color.Transparent;
		lblHours.BackColor = Color.Transparent;
		lblMinutes.BackColor = Color.Transparent;
		lblSeconds.BackColor = Color.Transparent;
		picPlayer.SizeMode = PictureBoxSizeMode.Zoom;
		picPlayer.Size = new Size(154, 97);
		picPlayer.Left = (pnlCassetteContainer.Width - 154) / 2;
		picPlayer.Top = 1;
		picPlayerNext.SizeMode = PictureBoxSizeMode.Zoom;
		picPlayerNext.Size = new Size(154, 97);
	}

	private void WireTimerLabels()
	{
		lblTasks.Click += delegate(object? s, EventArgs e) { btnAddTask_Click(s, e); };
		lblHours.Click += delegate(object? s, EventArgs e) { btnAddTime_Click(s, e); };
		lblMinutes.Click += delegate(object? s, EventArgs e) { btnAddTime_Click(s, e); };
		lblSeconds.Click += delegate(object? s, EventArgs e) { btnAddTime_Click(s, e); };
		lblHours.Paint += DrawGlow;
		lblMinutes.Paint += DrawGlow;
		lblSeconds.Paint += DrawGlow;
	}

	private void WireFormDrag()
	{
		if (lighthouseBar != null)
		{
			WireDragRecursive(lighthouseBar);
		}
	}

	private void WireDragRecursive(Control control)
	{
		control.MouseDown -= DragTargetMouseDown;
		control.MouseMove -= DragTargetMouseMove;
		control.MouseUp -= DragTargetMouseUp;
		control.MouseDown += DragTargetMouseDown;
		control.MouseMove += DragTargetMouseMove;
		control.MouseUp += DragTargetMouseUp;
		foreach (Control child in control.Controls)
		{
			WireDragRecursive(child);
		}
	}

	private void DragTargetMouseDown(object? sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			_isDraggingForm = true;
			_dragStartOffset = new Point(base.Location.X - Cursor.Position.X, base.Location.Y - Cursor.Position.Y);
		}
	}

	private void DragTargetMouseMove(object? sender, MouseEventArgs e)
	{
		if (sender is Control c)
		{
			c.Cursor = Cursors.SizeAll;
		}
		if (_isDraggingForm)
		{
			base.Location = new Point(Cursor.Position.X + _dragStartOffset.X, Cursor.Position.Y + _dragStartOffset.Y);
		}
	}

	private void DragTargetMouseUp(object? sender, MouseEventArgs e)
	{
		_isDraggingForm = false;
	}

	private void pnlGrip_Paint(object? sender, PaintEventArgs e)
	{
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using (var brush = new LinearGradientBrush(new RectangleF(0, 0, pnlGrip.Width, pnlGrip.Height),
			Color.FromArgb(34, 34, 38), Color.FromArgb(12, 12, 14), LinearGradientMode.Vertical))
			e.Graphics.FillRectangle(brush, pnlGrip.ClientRectangle);
		using (var top = new Pen(Color.FromArgb(50, 255, 255, 255), 1f))
			e.Graphics.DrawLine(top, 0, 0, pnlGrip.Width, 0);
		using (var groove = new SolidBrush(Color.FromArgb(60, 5, 5, 7)))
			e.Graphics.FillRectangle(groove, 0, pnlGrip.Height / 2 - 5, pnlGrip.Width, 10);
		int lineHeight = 2;
		int lineSpacing = 4;
		int totalHeight = 3 * lineHeight + 2 * lineSpacing;
		int startY = (pnlGrip.Height - totalHeight) / 2;
		using Pen linePen = new Pen(Color.FromArgb(90, 90, 94), lineHeight);
		linePen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
		linePen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
		int margin = 80;
		for (int i = 0; i < 3; i++)
		{
			int y = startY + i * (lineHeight + lineSpacing);
			e.Graphics.DrawLine(linePen, margin, y, pnlGrip.Width - margin, y);
		}
	}

	private void btnCloseApp_Click(object? sender, EventArgs e)
	{
		ShutdownEngine();
		Application.Exit();
	}

	private void ShutdownEngine()
	{
		try { _slideTimer?.Stop(); } catch { }
		try { _eqTimer?.Stop(); } catch { }
		try { _spriteTimer?.Stop(); } catch { }
		try { _metaTimer?.Stop(); } catch { }
		try { _m3u8WatchTimer?.Stop(); } catch { }
		try { _hlsPlayer?.Dispose(); } catch (Exception ex) { Logger.Error("Form1.ShutdownEngine.Hls", ex); }
		_hlsPlayer = null;
	}

	private void ShowColorMenu()
	{
		ContextMenuStrip menu = new ContextMenuStrip();
		menu.BackColor = Color.FromArgb(40, 40, 40);
		menu.ForeColor = Color.White;

		ToolStripMenuItem blancoItem = new ToolStripMenuItem("BLANCO");
		blancoItem.Click += delegate { ApplyAppColor(Color.White); };
		menu.Items.Add(blancoItem);

		ToolStripMenuItem negroItem = new ToolStripMenuItem("NEGRO");
		negroItem.Click += delegate { ApplyAppColor(Color.Black); };
		menu.Items.Add(negroItem);

		menu.Items.Add(new ToolStripSeparator());

		ToolStripMenuItem primarios = new ToolStripMenuItem("PRIMARIOS");
		ToolStripMenuItem rojo = new ToolStripMenuItem("ROJO", null, delegate { ApplyAppColor(Color.Red); });
		ToolStripMenuItem azul = new ToolStripMenuItem("AZUL", null, delegate { ApplyAppColor(Color.Blue); });
		ToolStripMenuItem amarillo = new ToolStripMenuItem("AMARILLO", null, delegate { ApplyAppColor(Color.Yellow); });
		primarios.DropDownItems.Add(rojo);
		primarios.DropDownItems.Add(azul);
		primarios.DropDownItems.Add(amarillo);
		menu.Items.Add(primarios);

		ToolStripMenuItem secundarios = new ToolStripMenuItem("SECUNDARIOS");
		ToolStripMenuItem verde = new ToolStripMenuItem("VERDE", null, delegate { ApplyAppColor(Color.Lime); });
		ToolStripMenuItem naranja = new ToolStripMenuItem("NARANJA", null, delegate { ApplyAppColor(Color.Orange); });
		ToolStripMenuItem violeta = new ToolStripMenuItem("VIOLETA", null, delegate { ApplyAppColor(Color.Violet); });
		secundarios.DropDownItems.Add(verde);
		secundarios.DropDownItems.Add(naranja);
		secundarios.DropDownItems.Add(violeta);
		menu.Items.Add(secundarios);

		menu.Items.Add(new ToolStripSeparator());

		ToolStripMenuItem cyan = new ToolStripMenuItem("CYAN", null, delegate { ApplyAppColor(Color.Cyan); });
		ToolStripMenuItem magenta = new ToolStripMenuItem("MAGENTA", null, delegate { ApplyAppColor(Color.Magenta); });
		ToolStripMenuItem coral = new ToolStripMenuItem("CORAL", null, delegate { ApplyAppColor(Color.FromArgb(255, 127, 80)); });
		ToolStripMenuItem teal = new ToolStripMenuItem("TEAL", null, delegate { ApplyAppColor(Color.Teal); });
		ToolStripMenuItem rosa = new ToolStripMenuItem("ROSA", null, delegate { ApplyAppColor(Color.HotPink); });
		ToolStripMenuItem dorado = new ToolStripMenuItem("DORADO", null, delegate { ApplyAppColor(Color.FromArgb(255, 215, 0)); });
		menu.Items.Add(cyan);
		menu.Items.Add(magenta);
		menu.Items.Add(coral);
		menu.Items.Add(teal);
		menu.Items.Add(rosa);
		menu.Items.Add(dorado);

		menu.Show(btnColor, new Point(0, btnColor.Height));
	}

	private void Form1_Load(object? sender, EventArgs e)
	{
		base.StartPosition = FormStartPosition.Manual;
		Screen? screen = Screen.PrimaryScreen;
		int screenWidth = screen?.WorkingArea.Width ?? 1920;
		base.Location = new Point(screenWidth - base.Width, 150);
		lblCassettes.Text = "CASSETTES";
		txtCassetteNum.Text = "0";
		lblCassetteTotal.Text = "/0";
		_eqTimer?.Start();
		this.BeginInvoke(new Action(() => LoadAllData()));
	}

	private void LoadAllData()
	{
		LoadCassetteMaster();
		_ = Task.Run(WarmCassetteCache);
		UpdateCassetteHeaderText();
		txtCassetteNum.Text = "0";
		lblCassetteTotal.Text = $"/{_cassettes.Count}";
		InitPlayer();
		LayoutCassetteHeader();
		LoadImageAssets();

		BuildProgressIndicator();
		Panel row1 = BuildTimeSelectorRow();
		Panel row2 = BuildPresetRow(HourPresetLabels, PresetUnit.Hours, 2);
		Panel row3 = BuildPresetRow(MinutePresetLabels, PresetUnit.Minutes, 3);
		Panel row4 = BuildStartStopRow();
		BuildToolsRow();
		ApplyCustomFontTo(row1, row2, row3, row4);

		ApplyLearningData();
		ApplyCurrentTheme();
		LoadThemesFile();
		if (_cassettes.Count > 0)
		{
			ApplyCassette(0);
		}
		else if (_m3uFiles.Count > 0)
		{
			LoadCurrentM3u();
		}
		SetVolumePreset(3);

		ReorganizeVolumePanel();
		BuildCassetteShortcutRow();
		LayoutCassetteStage();
		BuildAbcPresetKnobs();
		BuildTransportRow();
		BuildJackPanel();
		FinalizeFooterLayout();
	}

	private void LoadImageAssets()
	{
		Screen? screen = Screen.PrimaryScreen;
		try
		{
			string imgDir = PathHelper.GetImgDir();
			if (!Directory.Exists(imgDir)) return;
			LoadCassetteCoverImages(imgDir);
			LoadAnimationImages(imgDir);
			LoadThemeImages(imgDir);
			SetupSpriteOverlay(imgDir);
			ApplyDefaultTvFrame(imgDir, screen);
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.LoadAllData", ex);
		}
	}

	private void LoadCassetteCoverImages(string imgDir)
	{
		for (int j = 1; j <= 5; j++)
		{
			string pPath = Path.Combine(imgDir, $"cass00{j}.png");
			if (!File.Exists(pPath)) continue;
			try
			{
				_cassetteImages.Add(PathHelper.LoadImage(pPath));
			}
			catch (Exception ex)
			{
				Logger.Error("Form1.LoadAllData.CassetteImage", ex);
			}
		}
	}

	private void LoadAnimationImages(string imgDir)
	{
		foreach (string file in Directory.GetFiles(imgDir, "*_ANI.gif"))
		{
			if (file.EndsWith("noise_ANI.gif", StringComparison.OrdinalIgnoreCase) || file.EndsWith("noise_ANIgif.gif", StringComparison.OrdinalIgnoreCase))
			{
				_noiseFile = file;
			}
			else
			{
				_aniFiles.Add(file);
			}
		}
		if (_aniFiles.Count == 0) return;
		int defaultIndex = _aniFiles.FindIndex(f => f.EndsWith("cat_ANI.gif", StringComparison.OrdinalIgnoreCase));
		if (defaultIndex != -1)
		{
			_currentAniIndex = defaultIndex;
		}
		picMainDisplay.ImageLocation = _aniFiles[_currentAniIndex];
		picMainDisplay.SizeMode = PictureBoxSizeMode.Zoom;
		picMainDisplay.BackColor = Color.Transparent;
	}

	private void LoadThemeImages(string imgDir)
	{
		foreach (string file in Directory.GetFiles(imgDir, "*_TH.*"))
		{
			try
			{
				_themeFilePaths.Add(file);
				if (file.EndsWith("05_TH.gif", StringComparison.OrdinalIgnoreCase))
				{
					_currentThemeImageIndex = _themeFilePaths.Count - 1;
					_currentThemeImage = PathHelper.LoadImage(file);
					BackgroundImage = null;
				}
			}
			catch (Exception ex)
			{
				Logger.Error("Form1.LoadAllData.ThemeImage", ex);
			}
		}
		if (_currentThemeImage != null)
		{
			UpdateTVFrame(_themeFilePaths[_currentThemeImageIndex]);
		}
	}

	private void SetupSpriteOverlay(string imgDir)
	{
		string spritePath = Path.Combine(imgDir, "sprite_s.png");
		if (!File.Exists(spritePath)) return;
		LoadSpriteSheet(spritePath, 160, 160, 15);
		picOverlay.Parent = picMainDisplay;
		picOverlay.Location = new Point(0, 0);
		picOverlay.Size = picMainDisplay.Size;
		picOverlay.SizeMode = PictureBoxSizeMode.Zoom;
		picOverlay.BackColor = Color.Transparent;
		_spriteTimer = new Timer
		{
			Interval = 150
		};
		_spriteTimer.Tick += delegate
		{
			if (_spriteFrames.Count != 0)
			{
				_currentSpriteFrame = (_currentSpriteFrame + 1) % _spriteFrames.Count;
				picOverlay.Image = _spriteFrames[_currentSpriteFrame];
			}
		};
		_spriteTimer.Start();
	}

	private void ApplyDefaultTvFrame(string imgDir, Screen? screen)
	{
		string tvPath = Path.Combine(imgDir, "old_TV.gif");
		if (!File.Exists(tvPath)) return;
		Image tvImg = PathHelper.LoadImage(tvPath);
		SetTimerBackground(tvImg, false);
		timerPanel.BackgroundImageLayout = ImageLayout.None;
		base.Width = tvImg.Width + LighthouseWidth;
		timerPanel.Height = tvImg.Height - 4;
		timerPanel.BackColor = Color.Transparent;
		pnlTopButtons.BackColor = Color.Transparent;
		pnlTimerControls.BackColor = Color.Transparent;
		picMainDisplay.BackColor = Color.Transparent;
		btnP.BackColor = Color.Transparent;
		btnS.BackColor = Color.Transparent;
		ApplyRoundedFormRegion();
		if (screen != null)
		{
			base.Location = new Point(screen.WorkingArea.Width - base.Width, 150);
		}
	}

	private void BuildProgressIndicator()
	{
		Panel pnlTaskInfo = new Panel
		{
			Width = ContentWidth,
			Height = 20,
			BackColor = Color.Transparent,
			Margin = new Padding(0),
			Name = "pnlTaskInfo"
		};
		lblTaskInfo = new Label
		{
			Text = "---",
			TextAlign = ContentAlignment.MiddleCenter,
			Dock = DockStyle.Fill,
			BackColor = Color.Transparent,
			ForeColor = Color.Gray,
			Font = BuildNodeLabelFont()
		};
		lblTaskRemaining = new Label
		{
			Text = "",
			BackColor = Color.Transparent,
			ForeColor = Color.Gray
		};
		pnlProgressBg = new Panel
		{
			Height = 20,
			BackColor = Color.Transparent,
			Name = "pnlProgressBg",
			Dock = DockStyle.Bottom
		};
		pnlProgressFill = new BufferedPanel
		{
			Height = 20,
			BackColor = Color.Transparent,
			Name = "pnlProgressFill",
			Dock = DockStyle.Fill
		};
		InitProgressNodeGeometry(pnlTaskInfo.Width);
		pnlProgressFill.Paint += ProgressNodesPaint;
		pnlProgressBg.Controls.Add(pnlProgressFill);
		pnlTaskInfo.Controls.Add(pnlProgressBg);
		tasksListPanel.Controls.Add(pnlTaskInfo);

		pnlTopButtons.Controls.Remove(btnS);
		pnlTopButtons.Controls.Remove(btnP);
		base.Controls.Remove(picCincross);
		picCincross.Dock = DockStyle.Right;
		picCincross.SizeMode = PictureBoxSizeMode.Zoom;
		picCincross.Width = 85;
		pnlTopButtons.Controls.Add(picCincross);
	}

	private Font BuildNodeLabelFont()
	{
		try
		{
			if (_pfc.Families.Length > 0)
			{
				return new Font(_pfc.Families[0], 8f, FontStyle.Bold);
			}
		}
		catch { }
		return new Font("Segoe UI", 8f, FontStyle.Bold);
	}

	private void InitProgressNodeGeometry(int availableWidth)
	{
		_nodeCount = 16;
		int panelExtra = 8;
		int panelSize = NodeMaxBaseSize + panelExtra;
		int gap = 5;

		_nodeBaseSizes = new int[_nodeCount];
		_nodeIntensities = new double[_nodeCount];
		_nodeCenterX = new float[_nodeCount];

		int[] xPos = new int[_nodeCount];
		int xAcc = 0;
		for (int i = 0; i < _nodeCount; i++)
		{
			double pos = (double)i / (_nodeCount - 1);
			double baseScale = 1.0 + (NodeMaxScale - 1.0) * Math.Min(pos * 2, 1.0);
			_nodeBaseSizes[i] = (int)(NodeStandardSize * baseScale);
			xPos[i] = xAcc;
			xAcc += _nodeBaseSizes[i] + gap;
		}
		int shift = panelExtra / 2;
		int totalW = xPos[_nodeCount - 1] + panelSize;
		int startX = Math.Max(2, (availableWidth - totalW) / 2);
		for (int i = 0; i < _nodeCount; i++)
		{
			_nodeCenterX[i] = startX + xPos[i] - shift + panelSize / 2f;
		}
	}

	private void ProgressNodesPaint(object? s, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		float midY = pnlProgressFill.Height / 2f;
		Color neonColor = _appColor == Color.Transparent || _appColor == Color.Empty ? Color.White : _appColor;

		for (int i = 0; i < _nodeCount; i++)
		{
			double intensity = Math.Max(_nodeIntensities[i], 0.01);
			int sz = _nodeBaseSizes[i];
			float cx = _nodeCenterX[i];
			double posProgress = (double)(sz - NodeStandardSize) / (NodeMaxBaseSize - NodeStandardSize);
			float glowPad = 2f + sz / 3f;

			using (var path = new GraphicsPath())
			{
				path.AddEllipse(cx - sz / 2f - glowPad, midY - sz / 2f - glowPad, sz + glowPad * 2f, sz + glowPad * 2f);
				using (var brush = new PathGradientBrush(path))
				{
					brush.CenterColor = Color.FromArgb((int)(intensity * (60 + 120 * posProgress)), neonColor);
					brush.SurroundColors = new[] { Color.FromArgb(0, neonColor) };
					g.FillPath(brush, path);
				}
			}

			using (var fill = new SolidBrush(Color.FromArgb((int)(80 + intensity * 155), neonColor)))
				g.FillEllipse(fill, cx - sz / 2f, midY - sz / 2f, sz, sz);

			int whiteSz = Math.Max(1, sz * 3 / 5);
			using (var core = new SolidBrush(Color.FromArgb((int)(intensity * 100), Color.White)))
				g.FillEllipse(core, cx - whiteSz / 2f, midY - whiteSz / 2f, whiteSz, whiteSz);
		}
	}

	private Panel BuildTimeSelectorRow()
	{
		Panel row = new Panel { Height = 26, Width = ContentWidth, BackColor = Color.Transparent, Margin = new Padding(0) };
		lblTimeSelector = new Label
		{
			Text = "",
			Dock = DockStyle.Fill,
			ForeColor = Color.FromArgb(160, 160, 160),
			Font = new Font("Segoe UI", 8f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleCenter,
			BackColor = Color.Transparent
		};
		lblTimeSelector.Paint += TimeSelectorPaint;
		row.Controls.Add(lblTimeSelector);
		tasksListPanel.Controls.Add(row);
		tasksListPanel.Controls.SetChildIndex(row, 1);
		UpdateTimeSelectorInfo();
		return row;
	}

	private void TimeSelectorPaint(object? s, PaintEventArgs e)
	{
		if (s is not Label lbl || lbl.Text.Length == 0) return;
		Rectangle rect = lbl.ClientRectangle;
		TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
		TextRenderer.DrawText(e.Graphics, lbl.Text, lbl.Font,
			new Rectangle(rect.X + 1, rect.Y + 1, rect.Width, rect.Height),
			Color.FromArgb(120, 0, 0, 0), flags);
		TextRenderer.DrawText(e.Graphics, lbl.Text, lbl.Font, rect, lbl.ForeColor, flags);
	}

	private static readonly string[] HourPresetLabels = ["1H", "2H", "3H", "4H"];

	private static readonly string[] MinutePresetLabels = ["5M", "10M", "15M", "30M"];

	private const int PresetButtonWidth = 58;

	private const int PresetButtonGap = 5;

	private enum PresetUnit
	{
		Hours,
		Minutes
	}

	private Panel BuildPresetRow(string[] labels, PresetUnit unit, int childIndex)
	{
		Panel row = new Panel { Height = 26, Width = ContentWidth, BackColor = Color.Transparent, Margin = new Padding(0) };
		int total = PresetButtonWidth * labels.Length + PresetButtonGap * (labels.Length - 1);
		int startX = (ContentWidth - total) / 2;
		for (int i = 0; i < labels.Length; i++)
		{
			int value = int.Parse(labels[i][..^1]);
			Button btn = MakeRoundedBtn(labels[i]);
			btn.Click += (_, _) => OnPresetClicked(unit, btn, value);
			btn.Left = startX + i * (PresetButtonWidth + PresetButtonGap);
			btn.Top = 2;
			row.Controls.Add(btn);
		}
		tasksListPanel.Controls.Add(row);
		tasksListPanel.Controls.SetChildIndex(row, childIndex);
		return row;
	}

	private void OnPresetClicked(PresetUnit unit, Button btn, int value)
	{
		if (_timerRunning)
		{
			_ = ShowPresetBlockMessage();
			return;
		}
		_presetBlockMsg = false;
		if (unit == PresetUnit.Hours)
		{
			if (_hourPresetBtn == btn)
			{
				_manualHours = 0;
				SetPresetSelected(btn, false);
				_hourPresetBtn = null;
			}
			else
			{
				_manualHours = value;
				if (_hourPresetBtn != null) SetPresetSelected(_hourPresetBtn, false);
				SetPresetSelected(btn, true);
				_hourPresetBtn = btn;
			}
		}
		else if (_minPresetBtn == btn)
		{
			_manualMinutes = 0;
			SetPresetSelected(btn, false);
			_minPresetBtn = null;
		}
		else
		{
			_manualMinutes = value;
			if (_minPresetBtn != null) SetPresetSelected(_minPresetBtn, false);
			SetPresetSelected(btn, true);
			_minPresetBtn = btn;
		}
		UpdateTimeSelectorInfo();
	}

	private Panel BuildStartStopRow()
	{
		Panel row = new Panel { Height = 30, Width = ContentWidth, BackColor = Color.Transparent, Margin = new Padding(0) };
		int gap = 12;
		int startW = (ContentWidth - gap) / 2;

		Button btnStart = new Button
		{
			Text = "START",
			Size = new Size(startW, row.Height),
			Location = new Point(0, 0),
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent },
			BackColor = Color.Transparent,
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 8f, FontStyle.Bold),
			UseVisualStyleBackColor = false,
			TextAlign = ContentAlignment.MiddleCenter,
			Cursor = Cursors.Hand
		};
		btnStart.Paint += StartButtonPaint;
		btnStart.Click += (_, _) => StartFromSelection();

		Button btnStop = new Button
		{
			Text = "STOP",
			Size = new Size(ContentWidth - startW - gap, row.Height),
			Location = new Point(startW + gap, 0),
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent },
			BackColor = Color.Transparent,
			ForeColor = GetAccentColor(),
			Font = new Font("Segoe UI", 8f, FontStyle.Bold),
			UseVisualStyleBackColor = false,
			TextAlign = ContentAlignment.MiddleCenter,
			Cursor = Cursors.Hand
		};
		btnStop.Paint += StopButtonPaint;
		btnStop.Click += (_, _) => ResetFromSelection();

		row.Controls.Add(btnStart);
		row.Controls.Add(btnStop);
		tasksListPanel.Controls.Add(row);
		tasksListPanel.Controls.SetChildIndex(row, 0);
		return row;
	}

	private void StartButtonPaint(object? s, PaintEventArgs e)
	{
		if (s is not Button b) return;
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
		using (var border = new Pen(GetAccentColor(), 1.5f))
			e.Graphics.DrawRectangle(border, 0, 0, b.Width - 1, b.Height - 1);
		using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		using (var textBrush = new SolidBrush(Color.White))
			e.Graphics.DrawString(b.Text, b.Font, textBrush, new RectangleF(0, 0, b.Width, b.Height), fmt);
	}

	private void StopButtonPaint(object? s, PaintEventArgs e)
	{
		if (s is not Button b) return;
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
		using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		using (var textBrush = new SolidBrush(GetAccentColor()))
			e.Graphics.DrawString(b.Text, b.Font, textBrush, new RectangleF(0, 0, b.Width, b.Height), fmt);
	}

	private void StartFromSelection()
	{
		_manualTotalSeconds = _manualHours * 3600 + _manualMinutes * 60;
		_timeRemaining = TimeSpan.FromSeconds(_manualTotalSeconds);
		UpdateTimerDisplay();
		if (_timeRemaining.TotalSeconds > 0)
			StartTimer();
		UpdateTaskInfo();
		UpdateTimeSelectorInfo();
	}

	private void ResetFromSelection()
	{
		StopTimer();
		_timeRemaining = TimeSpan.Zero;
		_manualTotalSeconds = 0;
		_manualHours = 0;
		_manualMinutes = 0;
		ClearPresetSelections();
		UpdateTimerDisplay();
		UpdateTaskInfo();
		_presetBlockMsg = false;
		UpdateTimeSelectorInfo();
	}

	private void ApplyCustomFontTo(params Control[] controls)
	{
		foreach (Control c in controls)
		{
			FontHelper.ApplyFont(c, 8f, FontStyle.Bold);
		}
	}

	private void ApplyLearningData()
	{
		LearningData? loaded = LearningData.Load();
		if (loaded != null)
			_learningData = loaded;
	}

	private void BuildToolsRow()
	{
		const int toolMargin = 6;
		const int toolGap = 4;
		int toolW = (ContentWidth - toolMargin * 2 - toolGap * 2) / 3;

		Button btnOpenPdf = CreateToolButton("PDF", toolW);
		Button btnYoutube = CreateToolButton("YT", toolW);

		string pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "files", "pdf", "file.pdf");
		btnOpenPdf.Click += (_, _) =>
		{
			if (!File.Exists(pdfPath)) return;
			using var form = new FormPdfViewer(pdfPath, this);
			form.ShowDialog();
		};
		btnYoutube.Click += (_, _) =>
		{
			using var form = new FormYoutube(this);
			form.ShowDialog();
		};

		btnLearn.Text = "LEARN";
		btnLearn.Width = toolW;
		btnLearn.Height = 22;
		btnLearn.Font = new Font("Segoe UI", 7f, FontStyle.Bold);
		btnLearn.BackColor = Color.Transparent;
		btnLearn.ForeColor = Color.FromArgb(200, 200, 200);
		btnLearn.UseVisualStyleBackColor = false;
		ApplyRoundedRegion(btnLearn);

		btnOpenPdf.Location = new Point(toolMargin, 3);
		btnYoutube.Location = new Point(toolMargin + toolW + toolGap, 3);
		btnLearn.Location = new Point(toolMargin + 2 * (toolW + toolGap), 3);
		toolsRow.Controls.Add(btnOpenPdf);
		toolsRow.Controls.Add(btnYoutube);
		toolsRow.Controls.Add(btnLearn);
		FontHelper.ApplyFont(btnOpenPdf, 7f, FontStyle.Bold);
		FontHelper.ApplyFont(btnYoutube, 7f, FontStyle.Bold);
		FontHelper.ApplyFont(btnLearn, 7f, FontStyle.Bold);
	}

	private Button CreateToolButton(string text, int width)
	{
		Button btn = new Button
		{
			Text = text,
			Width = width,
			Height = 22,
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0 },
			BackColor = Color.Transparent,
			ForeColor = Color.FromArgb(200, 200, 200),
			Font = new Font("Segoe UI", 7f, FontStyle.Bold),
			UseVisualStyleBackColor = false,
			TextAlign = ContentAlignment.MiddleCenter,
			Cursor = Cursors.Hand
		};
		ApplyRoundedRegion(btn);
		return btn;
	}

	private void ReorganizeVolumePanel()
	{
		pnlVolume.Controls.Remove(btnPlayPlayer);
		pnlVolume.Controls.Remove(btnStopPlayer);
		pnlVolume.Controls.Remove(pnlVolumeLine);
		pnlVolumeLine.Controls.Remove(pnlVolumeThumb);
		pnlVolume.Controls.Remove(btnVolLow);
		pnlVolume.Controls.Remove(btnVolMid);
		pnlVolume.Controls.Remove(btnVolMax);

		playerFooterPanel.Height = 292;
		pnlVolume.Location = new Point(0, 290);
		pnlVolume.Size = new Size(ContentWidth, 24);

		base.Controls.Remove(pnlFullListRow);
		pnlFullListRow.Controls.Remove(btnFavList);
		pnlFullListRow.Controls.Remove(btnCassetteList);
		cassettesHeaderPanel.Controls.Remove(txtCassetteNum);
		cassettesHeaderPanel.Controls.Remove(lblCassetteTotal);
		btnCassetteList.Text = "\u25A4";
		btnCassetteList.Font = new Font("Segoe UI Symbol", 10f, FontStyle.Bold);
		btnCassetteList.Tag = true;
		btnCassetteList.Location = new Point(159, 3);
		btnCassetteList.Size = new Size(72, 22);
		ApplyRoundedRegion(btnCassetteList);
		btnCassetteList.Paint += PresetButtonPaint;
		txtCassetteNum.Location = new Point(70, 3);
		lblCassetteTotal.Location = new Point(70 + txtCassetteNum.Width + 2, 6);
		pnlVolume.Controls.Add(txtCassetteNum);
		pnlVolume.Controls.Add(lblCassetteTotal);
		pnlVolume.Controls.Add(btnCassetteList);
	}

	private void BuildCassetteShortcutRow()
	{
		const int rowH = 36;
		const int gap = 4;
		const int count = 4;
		int w = (ContentWidth - gap * (count - 1)) / count;
		int startX = (ContentWidth - (w * count + gap * (count - 1))) / 2;
		string[] labels = ["FULL LIST", "\u2764", "M3U"];
		int[] widths = [w * 2 + gap, w, w];
		string[] names = ["btnCassFullList", "btnCassHeart", "btnCassM3u"];

		int x = startX;
		for (int i = 0; i < labels.Length; i++)
		{
			Button btn = MakeRoundedBtn(labels[i]);
			btn.Name = names[i];
			btn.Size = new Size(widths[i], rowH);
			btn.Font = new Font(i == 1 ? "Segoe UI Emoji" : "Segoe UI", i == 1 ? 14f : 9f, FontStyle.Bold);
			btn.Tag = true;
			btn.Location = new Point(x, 0);
			x += widths[i] + gap;
			ApplyRoundedRegion(btn);
			if (i == 0) btn.Click += (_, _) => ShowTagsBrowser();
			playerFooterPanel.Controls.Add(btn);
		}
	}

	private void LayoutCassetteStage()
	{
		pnlCassetteContainer.Location = new Point(0, 62);
		pnlCassetteContainer.Size = new Size(ContentWidth, 163);
		picPlayer.Location = new Point(4, 19);
		picPlayer.Size = new Size(198, 125);
		picPlayer.SizeMode = PictureBoxSizeMode.Zoom;
		picPlayerNext.Visible = false;
	}

	private void BuildAbcPresetKnobs()
	{
		int x = ContentWidth - 53;
		int y = 19;
		int w = 49;
		int h = 39;
		int gap = 4;
		string[] names = ["btnAbcA", "btnAbcB", "btnAbcC"];
		string[] labels = ["\u273A", "\u2738", "\u2736"];
		int[] presets = [15, 9, 3];

		pnlCassetteContainer.Paint += (_, e) => AbcGrooveBackPaint(e.Graphics, x, y, w, h, gap);
		for (int i = 0; i < 3; i++)
		{
			Button abc = new Button
			{
				Name = names[i],
				Text = labels[i],
				Left = x,
				Top = y + i * (h + gap),
				Width = w,
				Height = h,
				FlatStyle = FlatStyle.Flat,
				FlatAppearance = { BorderSize = 0 },
				BackColor = Color.Transparent,
				ForeColor = Color.White,
				Font = new Font("Segoe UI Emoji", 12f, FontStyle.Bold),
				UseVisualStyleBackColor = false,
				TextAlign = ContentAlignment.MiddleCenter,
				Cursor = Cursors.Hand,
				Tag = i == 2
			};
			abc.Paint += AbcGroovePaint;
			if (i == 0) btnAbcA = abc;
			else if (i == 1) btnAbcB = abc;
			else btnAbcC = abc;
			int preset = presets[i];
			abc.Click += (_, _) => SetVolumePreset(preset);
			pnlCassetteContainer.Controls.Add(abc);
		}
	}

	private void BuildTransportRow()
	{
		const int y = 226;
		const int h = 36;
		const int gap = 4;
		const int count = 4;
		int w = (ContentWidth - gap * (count - 1)) / count;
		int startX = (ContentWidth - (w * count + gap * (count - 1))) / 2;
		string[] glyphs = ["\uE892", "\uE768", "\uE71A", "\uE893"];
		string[] names = ["btnCassPrev", "btnCassPlay", "btnCassStop", "btnCassNext"];

		for (int i = 0; i < count; i++)
		{
			Button btn = MakeRoundedBtn(glyphs[i]);
			btn.Name = names[i];
			btn.Size = new Size(w, h);
			btn.Font = new Font("Segoe MDL2 Assets", 11f, FontStyle.Bold);
			btn.Tag = true;
			btn.Location = new Point(startX + i * (w + gap), y);
			ApplyRoundedRegion(btn);
			switch (i)
			{
				case 0: btn.Click += (_, _) => ChangeCassette(-1); break;
				case 1: btn.Click += async (_, _) => await ResumePlaybackAsync(); break;
				case 2: btn.Click += (_, _) => StopPlayback(); break;
				case 3: btn.Click += (_, _) => ChangeCassette(1); break;
			}
			playerFooterPanel.Controls.Add(btn);
		}
	}

	private void BuildJackPanel()
	{
		pnlEqualizer.Location = new Point(0, 264);
		pnlEqualizer.Size = new Size(ContentWidth, 24);
		lblM3uTitle.Location = new Point(-500, -500);
		lblM3uTitle.Visible = true;
		pnlStationTitle = new BufferedPanel
		{
			Name = "pnlStationTitle",
			Location = new Point(0, 38),
			Size = new Size(ContentWidth, 24),
			BackColor = Color.Black
		};
		pnlStationTitle.Paint += (_, e) => DrawStationTitleOverlay(e.Graphics, pnlStationTitle!.Width, pnlStationTitle.Height);
		playerFooterPanel.Controls.Add(pnlStationTitle);
		lblM3uTitle.TextChanged += (_, _) => pnlStationTitle?.Invalidate();
		lblMetadata.Location = new Point(-500, -500);
		lblExtraMetadata.Location = new Point(-500, -500);
		pnlVolume.Location = new Point(5, 290);
		pnlVolume.Visible = false;

		Label lblNro = new Label
		{
			Name = "lblNro",
			Text = "NRO",
			Location = new Point(5, 1),
			Size = new Size(46, 20),
			AutoSize = false,
			BackColor = Color.Black,
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 8f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleCenter
		};
		lblNro.BringToFront();
		pnlVolume.Controls.Add(lblNro);

		Button[] knobs = [btnVolLow, btnVolMid, btnVolMax];
		foreach (Button knob in knobs)
		{
			knob.Paint += VolumeKnobPaint;
			knob.Invalidate();
		}

		Color[] jackColors =
		[
			Color.FromArgb(255, 80, 80),
			Color.FromArgb(235, 235, 235),
			Color.FromArgb(120, 220, 120),
			Color.FromArgb(90, 150, 255),
			Color.FromArgb(255, 210, 80),
			Color.FromArgb(255, 140, 60)
		];
		for (int i = 0; i < jackColors.Length; i++)
		{
			Panel jack = new Panel
			{
				Name = "jack" + (i + 1),
				Location = new Point(78 + i * 28, 101),
				Size = new Size(20, 20),
				BackColor = Color.Transparent,
				Tag = jackColors[i]
			};
			jack.Paint += JackSocketPaint;
			pnlVolume.Controls.Add(jack);
		}

		picPlayer.Paint += CassetteGlassPaint;
		picPlayerNext.Paint += CassetteGlassPaint;
	}

	private void FinalizeFooterLayout()
	{
		btnCloseApp.Height = 60;
		btnCloseApp.Dock = DockStyle.Bottom;
		base.ClientSize = new Size(base.Width, base.ClientSize.Height + 22);
		ApplyRoundedFormRegion();
		ApplyRadioSkin();
	}

	private Button MakeRoundedBtn(string text)
	{
		Button btn = new Button
		{
			Text = text,
			Width = 58,
			Height = 25,
			FlatStyle = FlatStyle.Flat,
			FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent },
			BackColor = Color.FromArgb(50, 50, 50),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 8f, FontStyle.Bold),
			UseVisualStyleBackColor = false,
			TextAlign = ContentAlignment.MiddleCenter,
			Cursor = Cursors.Hand,
			Tag = false
		};
		ApplyRoundedRegion(btn);
		btn.Paint += PresetButtonPaint;
		return btn;
	}

	private void PresetButtonPaint(object? s, PaintEventArgs e)
	{
		if (s is Button b)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
			bool selected = b.Tag is bool sel && sel;
			RectangleF rect = new RectangleF(1f, 1f, b.Width - 2f, b.Height - 2f);
			float radius = 6f;
			using (var path = RoundedPath(rect, radius))
			{
				if (selected)
				{
					Color accent = GetAccentColor();
					Color top = ControlPaint.Light(accent, 0.45f);
					Color bottom = ControlPaint.Dark(accent, 0.3f);
					using (var fill = new LinearGradientBrush(rect, top, bottom, LinearGradientMode.Vertical))
						e.Graphics.FillPath(fill, path);
					using (var halo = new Pen(Color.FromArgb(120, accent), 2.5f))
						e.Graphics.DrawPath(halo, path);
					RectangleF innerRect = new RectangleF(3.5f, 3.5f, b.Width - 7f, b.Height - 7f);
					using (var innerPath = RoundedPath(innerRect, radius - 1.5f))
					{
						using (var glow = new LinearGradientBrush(innerRect,
							Color.FromArgb(170, ControlPaint.Light(accent)),
							Color.FromArgb(50, accent), LinearGradientMode.Vertical))
							e.Graphics.FillPath(glow, innerPath);
						using (var ring = new Pen(Color.FromArgb(200, Color.White), 1f))
							e.Graphics.DrawPath(ring, innerPath);
					}
				}
				else
				{
					using (var fill = new LinearGradientBrush(rect,
						Color.FromArgb(82, 82, 86), Color.FromArgb(36, 36, 40), LinearGradientMode.Vertical))
						e.Graphics.FillPath(fill, path);
					using (var sheen = new LinearGradientBrush(rect,
						Color.FromArgb(60, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical))
						e.Graphics.FillPath(sheen, path);
					using (var edge = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
						e.Graphics.DrawPath(edge, path);
					using (var border = new Pen(Color.FromArgb(120, 0, 0, 0), 1f))
						e.Graphics.DrawPath(border, path);
				}
			}
			Color fg = selected ? Color.White : Color.FromArgb(205, 205, 205);
			using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
			{
				if (selected)
				{
					Color accent = GetAccentColor();
					for (int i = 1; i <= 2; i++)
					{
						using var glowBrush = new SolidBrush(Color.FromArgb(70 / i, accent));
						e.Graphics.DrawString(b.Text, b.Font, glowBrush, new RectangleF(i, i, b.Width - 2, b.Height - 2), fmt);
						e.Graphics.DrawString(b.Text, b.Font, glowBrush, new RectangleF(-i, -i, b.Width - 2, b.Height - 2), fmt);
					}
				}
				using (var textBrush = new SolidBrush(fg))
					e.Graphics.DrawString(b.Text, b.Font, textBrush, new RectangleF(1, 1, b.Width - 2, b.Height - 2), fmt);
			}
		}
	}

	private void SetPresetSelected(Button b, bool selected)
	{
		b.Tag = selected;
		b.Invalidate();
	}

	private void ClearPresetSelections()
	{
		if (_hourPresetBtn != null)
		{
			SetPresetSelected(_hourPresetBtn, false);
			_hourPresetBtn = null;
		}
		if (_minPresetBtn != null)
		{
			SetPresetSelected(_minPresetBtn, false);
			_minPresetBtn = null;
		}
	}

	private Color GetAccentColor()
	{
		if (_appColor != Color.Transparent && _appColor != Color.Empty)
			return _appColor;
		if (_currentThemeImage != null)
			return Color.FromArgb(100, 100, 100);
		return _themes[_currentThemeIndex].Item1;
	}

	private static GraphicsPath RoundedPath(RectangleF r, float radius)
	{
		GraphicsPath path = new GraphicsPath();
		float d = radius * 2f;
		path.AddArc(r.X, r.Y, d, d, 180, 90);
		path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
		path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
		path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
		path.CloseFigure();
		return path;
	}

	private static Color DarkenColor(Color c, double factor)
	{
		int r = (int)Math.Max(0, c.R * (1 - factor));
		int g = (int)Math.Max(0, c.G * (1 - factor));
		int b = (int)Math.Max(0, c.B * (1 - factor));
		return Color.FromArgb(c.A, r, g, b);
	}

	private void InvalidatePresetButtons()
	{
		foreach (Control row in tasksListPanel.Controls)
		{
			if (row is Panel p)
			{
				foreach (Control c in p.Controls)
				{
					if (c is Button b)
						b.Invalidate();
				}
			}
		}
	}

	private void ApplyRoundedRegion(Control c)
	{
		using var path = new System.Drawing.Drawing2D.GraphicsPath();
		int r = 6;
		path.AddArc(0, 0, r * 2, r * 2, 180, 90);
		path.AddArc(c.Width - r * 2, 0, r * 2, r * 2, 270, 90);
		path.AddArc(c.Width - r * 2, c.Height - r * 2, r * 2, r * 2, 0, 90);
		path.AddArc(0, c.Height - r * 2, r * 2, r * 2, 90, 90);
		path.CloseFigure();
		c.Region = new Region(path);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			try { _learningData?.Save(); } catch (Exception ex) { Logger.Error("Form1.Dispose.Learning", ex); }
			try { _hlsPlayer?.Dispose(); } catch (Exception ex) { Logger.Error("Form1.Dispose.Hls", ex); }
			_hlsPlayer = null;
			try
			{
				_slideTimer?.Stop();
				_eqTimer?.Stop();
				_spriteTimer?.Stop();
				_metaTimer?.Stop();
				_m3u8WatchTimer?.Stop();
			}
			catch (Exception ex) { Logger.Error("Form1.Dispose.StopTimers", ex); }
			try
			{
				picMainDisplay.ImageLocation = null;
				picOverlay.Image = null;
				picPlayer.Image = null;
				picMainDisplay.Image = null;
				timerPanel.BackgroundImage = null;
			}
			catch (Exception ex) { Logger.Error("Form1.Dispose.DetachImages", ex); }
			try
			{
				_slideTimer?.Dispose();
				_eqTimer?.Dispose();
				_spriteTimer?.Dispose();
				_metaTimer?.Dispose();
				_m3u8WatchTimer?.Dispose();
				_pfc?.Dispose();
			}
			catch (Exception ex) { Logger.Error("Form1.Dispose.Timers", ex); }
			try
			{
				_currentThemeImage?.Dispose();
				if (_nextCassetteImage != null && !ReferenceEquals(_nextCassetteImage, _playerImage))
				{
					_nextCassetteImage.Dispose();
				}
				_nextCassetteImage = null;
				_playerImage?.Dispose();
				_playerImage = null;
				foreach (var img in _cassetteImages) img.Dispose();
				_cassetteImages.Clear();
				lock (_imageCacheLock)
				{
					foreach (var img in _imageCache.Values) img.Dispose();
					_imageCache.Clear();
				}
				ClearFrameList(_fadeOutFrames);
				ClearFrameList(_fadeInFrames);
				if (!_timerBackgroundCached && timerPanel.BackgroundImage != null)
					timerPanel.BackgroundImage.Dispose();
				foreach (var bmp in _spriteFrames) bmp.Dispose();
				_spriteFrames.Clear();
			}
			catch (Exception ex) { Logger.Error("Form1.Dispose.Images", ex); }
			if (_wmp != null)
			{
				try { System.Runtime.InteropServices.Marshal.ReleaseComObject(_wmp); }
				catch (Exception ex) { Logger.Error("Form1.Dispose.Wmp", ex); }
				_wmp = null;
			}
			if (_wmpAlarm != null)
			{
				try { System.Runtime.InteropServices.Marshal.ReleaseComObject(_wmpAlarm); }
				catch (Exception ex) { Logger.Error("Form1.Dispose.WmpAlarm", ex); }
				_wmpAlarm = null;
			}
			try { components?.Dispose(); } catch (Exception ex) { Logger.Error("Form1.Dispose.Components", ex); }
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this.timerPanel = new BufferedPanel();
		this.picOverlay = new System.Windows.Forms.PictureBox();
		this.picMainDisplay = new System.Windows.Forms.PictureBox();
		this.pnlTimerControls = new System.Windows.Forms.Panel();
		this.btnTheme = new System.Windows.Forms.Button();
		this.btnImage = new System.Windows.Forms.Button();
		this.btnColor = new System.Windows.Forms.Button();
		this.pnlTopButtons = new BufferedPanel();
		this.lblHours = new BufferedLabel();
		this.lblMinutes = new BufferedLabel();
		this.lblSeconds = new BufferedLabel();
		this.btnP = new System.Windows.Forms.Button();
		this.btnS = new System.Windows.Forms.Button();
		this.countdownTimer = new System.Windows.Forms.Timer(this.components);
		this.tasksHeaderPanel = new BufferedPanel();
		this.lblTasks = new System.Windows.Forms.Label();
		this.toolsHeaderPanel = new BufferedPanel();
		this.lblTools = new System.Windows.Forms.Label();
		this.toolsRow = new System.Windows.Forms.Panel();
		this.cassettesHeaderPanel = new BufferedPanel();
		this.lblCassettes = new System.Windows.Forms.Label();
		this.lblCassetteCount = new System.Windows.Forms.Label();
		this.playerFooterPanel = new BufferedPanel();
		this.pnlEqualizer = new BufferedPanel();
		this.pnlVolume = new System.Windows.Forms.Panel();
		this.btnStopPlayer = new System.Windows.Forms.Button();
		this.btnPlayPlayer = new System.Windows.Forms.Button();
		this.pnlVolumeLine = new System.Windows.Forms.Panel();
		this.pnlVolumeThumb = new System.Windows.Forms.Panel();
		this.btnVolLow = new System.Windows.Forms.Button();
		this.btnVolMid = new System.Windows.Forms.Button();
		this.btnVolMax = new System.Windows.Forms.Button();
		this.pnlCassetteContainer = new BufferedPanel();
		this.picPlayer = new System.Windows.Forms.PictureBox();
		this.picPlayerNext = new System.Windows.Forms.PictureBox();
		this.lblM3uTitle = new System.Windows.Forms.Label();
		this.lblMetadata = new System.Windows.Forms.Label();
		this.lblExtraMetadata = new System.Windows.Forms.Label();
		this.tasksListPanel = new BufferedFlowLayoutPanel();
		this.pnlGrip = new BufferedPanel();
		this.btnCloseApp = new System.Windows.Forms.Button();
		this.picCincross = new System.Windows.Forms.PictureBox();
		this.timerPanel.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.picPlayer).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.picPlayerNext).BeginInit();
		base.SuspendLayout();
		this.pnlGrip.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
		this.pnlGrip.Dock = System.Windows.Forms.DockStyle.Top;
		this.pnlGrip.Height = 20;
		this.pnlGrip.Name = "pnlGrip";
		this.pnlGrip.TabIndex = 6;
		this.pnlGrip.Visible = false;
		this.pnlGrip.Paint += new System.Windows.Forms.PaintEventHandler(pnlGrip_Paint);
		this.btnCloseApp.BackColor = System.Drawing.Color.Black;
		this.btnCloseApp.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.btnCloseApp.FlatAppearance.BorderSize = 0;
		this.btnCloseApp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnCloseApp.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
		this.btnCloseApp.ForeColor = System.Drawing.Color.White;
		this.btnCloseApp.Height = 60;
		this.btnCloseApp.Name = "btnCloseApp";
		this.btnCloseApp.TabIndex = 7;
		this.btnCloseApp.Text = "X CLOSE";
		this.btnCloseApp.UseVisualStyleBackColor = false;
		this.btnCloseApp.Click += new System.EventHandler(btnCloseApp_Click);
		string cincrossPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "files", "img", "cin_LOGO.png");
		if (System.IO.File.Exists(cincrossPath))
		{
			this.picCincross.Image = PathHelper.LoadImage(cincrossPath);
		}
		this.picCincross.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
		this.picCincross.Name = "picCincross";
		this.picCincross.TabIndex = 8;
		this.picCincross.TabStop = false;
		this.timerPanel.BackColor = System.Drawing.Color.Black;
		this.timerPanel.Controls.Add(this.picOverlay);
		this.timerPanel.Controls.Add(this.picMainDisplay);
		this.timerPanel.Controls.Add(this.pnlTimerControls);
		this.timerPanel.Controls.Add(this.pnlTopButtons);
		this.timerPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.timerPanel.Location = new System.Drawing.Point(0, 0);
		this.timerPanel.Name = "timerPanel";
		this.timerPanel.Size = new System.Drawing.Size(260, 200);
		this.timerPanel.TabIndex = 0;
		this.picOverlay.BackColor = System.Drawing.Color.Transparent;
		this.picOverlay.Location = new System.Drawing.Point(10, 40);
		this.picOverlay.Name = "picOverlay";
		this.picOverlay.Size = new System.Drawing.Size(160, 160);
		this.picOverlay.TabIndex = 6;
		this.picOverlay.TabStop = false;
		this.picMainDisplay.BackColor = System.Drawing.Color.Black;
		this.picMainDisplay.Location = new System.Drawing.Point(10, 40);
		this.picMainDisplay.Name = "picMainDisplay";
		this.picMainDisplay.Size = new System.Drawing.Size(160, 160);
		this.picMainDisplay.TabIndex = 5;
		this.picMainDisplay.TabStop = false;
		this.pnlTimerControls.Controls.Add(this.btnTheme);
		this.pnlTimerControls.Controls.Add(this.btnImage);
		this.pnlTimerControls.Controls.Add(this.btnColor);
		this.pnlTimerControls.Dock = System.Windows.Forms.DockStyle.Right;
		this.pnlTimerControls.Location = new System.Drawing.Point(180, 40);
		this.pnlTimerControls.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
		this.pnlTimerControls.Name = "pnlTimerControls";
		this.pnlTimerControls.Size = new System.Drawing.Size(80, 160);
		this.pnlTimerControls.TabIndex = 4;
		this.btnTheme.FlatAppearance.BorderSize = 0;
		this.btnTheme.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnTheme.Font = new System.Drawing.Font("Segoe UI", 6f);
		this.btnTheme.ForeColor = System.Drawing.Color.White;
		this.btnTheme.Location = new System.Drawing.Point(0, 50);
		this.btnTheme.Name = "btnTheme";
		this.btnTheme.Size = new System.Drawing.Size(80, 25);
		this.btnTheme.TabIndex = 3;
		this.btnTheme.Text = "THEME";
		this.btnTheme.UseVisualStyleBackColor = true;
		this.btnAddFav = new Button();
		this.btnAddFav.FlatAppearance.BorderSize = 0;
		this.btnAddFav.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnAddFav.Font = new System.Drawing.Font("Segoe UI", 6f);
		this.btnAddFav.ForeColor = System.Drawing.Color.White;
		this.btnAddFav.Location = new System.Drawing.Point(0, 75);
		this.btnAddFav.Name = "btnAddFav";
		this.btnAddFav.Size = new System.Drawing.Size(80, 25);
		this.btnAddFav.TabIndex = 4;
		this.btnAddFav.Text = "ADD FAV";
		this.btnAddFav.UseVisualStyleBackColor = true;
		this.btnAddFav.Click += new EventHandler(btnAddFav_Click);
		this.btnLearn = new Button();
		this.btnLearn.FlatAppearance.BorderSize = 0;
		this.btnLearn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnLearn.Font = new System.Drawing.Font("Segoe UI", 6f);
		this.btnLearn.ForeColor = System.Drawing.Color.White;
		this.btnLearn.Location = new System.Drawing.Point(0, 100);
		this.btnLearn.Name = "btnLearn";
		this.btnLearn.Size = new System.Drawing.Size(80, 25);
		this.btnLearn.TabIndex = 6;
		this.btnLearn.Text = "LEARN";
		this.btnLearn.UseVisualStyleBackColor = true;
		this.btnInfo = new Button();
		this.btnInfo.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
		this.btnInfo.FlatAppearance.BorderSize = 0;
		this.btnInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnInfo.Font = new System.Drawing.Font("Segoe UI", 6f);
		this.btnInfo.ForeColor = System.Drawing.Color.White;
		this.btnInfo.Location = new System.Drawing.Point(0, 75);
		this.btnInfo.Name = "btnInfo";
		this.btnInfo.Size = new System.Drawing.Size(80, 25);
		this.btnInfo.TabIndex = 7;
		this.btnInfo.Text = "+ INFO";
		this.btnInfo.UseVisualStyleBackColor = false;
		this.btnInfo.Click += new EventHandler(btnInfo_Click);
		this.pnlTimerControls.Controls.Add(this.btnInfo);
		this.btnImage.FlatAppearance.BorderSize = 0;
		this.btnImage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnImage.Font = new System.Drawing.Font("Segoe UI", 6f);
		this.btnImage.ForeColor = System.Drawing.Color.White;
		this.btnImage.Location = new System.Drawing.Point(0, 25);
		this.btnImage.Name = "btnImage";
		this.btnImage.Size = new System.Drawing.Size(80, 25);
		this.btnImage.TabIndex = 2;
		this.btnImage.Text = "IMAGE";
		this.btnImage.UseVisualStyleBackColor = true;
		this.btnColor.FlatAppearance.BorderSize = 0;
		this.btnColor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnColor.Font = new System.Drawing.Font("Segoe UI", 6f);
		this.btnColor.ForeColor = System.Drawing.Color.White;
		this.btnColor.Location = new System.Drawing.Point(0, 0);
		this.btnColor.Name = "btnColor";
		this.btnColor.Size = new System.Drawing.Size(80, 25);
		this.btnColor.TabIndex = 0;
		this.btnColor.Text = "COLOR";
		this.btnColor.UseVisualStyleBackColor = true;
		this.pnlTopButtons.BackColor = System.Drawing.Color.Black;
		this.pnlTopButtons.Controls.Add(this.lblHours);
		this.pnlTopButtons.Controls.Add(this.lblMinutes);
		this.pnlTopButtons.Controls.Add(this.lblSeconds);
		this.pnlTopButtons.Controls.Add(this.btnP);
		this.pnlTopButtons.Controls.Add(this.btnS);
		this.pnlTopButtons.Dock = System.Windows.Forms.DockStyle.Top;
		this.pnlTopButtons.Location = new System.Drawing.Point(0, 0);
		this.pnlTopButtons.Name = "pnlTopButtons";
		this.pnlTopButtons.Size = new System.Drawing.Size(260, 40);
		this.pnlTopButtons.TabIndex = 3;
		this.lblHours.AutoSize = true;
		this.lblHours.Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold);
		this.lblHours.ForeColor = System.Drawing.Color.White;
		this.lblHours.Location = new System.Drawing.Point(14, 5);
		this.lblHours.Name = "lblHours";
		this.lblHours.Size = new System.Drawing.Size(42, 32);
		this.lblHours.TabIndex = 2;
		this.lblHours.Text = "00";
		this.lblMinutes.AutoSize = true;
		this.lblMinutes.Font = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold);
		this.lblMinutes.ForeColor = System.Drawing.Color.White;
		this.lblMinutes.Location = new System.Drawing.Point(64, 2);
		this.lblMinutes.Name = "lblMinutes";
		this.lblMinutes.Size = new System.Drawing.Size(56, 45);
		this.lblMinutes.TabIndex = 3;
		this.lblMinutes.Text = "00";
		this.lblSeconds.AutoSize = true;
		this.lblSeconds.Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold);
		this.lblSeconds.ForeColor = System.Drawing.Color.White;
		this.lblSeconds.Location = new System.Drawing.Point(124, 5);
		this.lblSeconds.Name = "lblSeconds";
		this.lblSeconds.Size = new System.Drawing.Size(42, 32);
		this.lblSeconds.TabIndex = 4;
		this.lblSeconds.Text = "00";
		this.btnP.BackColor = System.Drawing.Color.Black;
		this.btnP.Dock = System.Windows.Forms.DockStyle.Right;
		this.btnP.FlatAppearance.BorderSize = 0;
		this.btnP.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnP.Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold);
		this.btnP.ForeColor = System.Drawing.Color.White;
		this.btnP.Location = new System.Drawing.Point(180, 0);
		this.btnP.Name = "btnP";
		this.btnP.Size = new System.Drawing.Size(40, 40);
		this.btnP.TabIndex = 1;
		this.btnP.Text = "▶";
		this.btnP.UseVisualStyleBackColor = false;
		this.btnS.BackColor = System.Drawing.Color.Black;
		this.btnS.Dock = System.Windows.Forms.DockStyle.Right;
		this.btnS.FlatAppearance.BorderSize = 0;
		this.btnS.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnS.Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold);
		this.btnS.ForeColor = System.Drawing.Color.White;
		this.btnS.Location = new System.Drawing.Point(220, 0);
		this.btnS.Name = "btnS";
		this.btnS.Size = new System.Drawing.Size(40, 40);
		this.btnS.TabIndex = 0;
		this.btnS.Text = "⏹";
		this.btnS.UseVisualStyleBackColor = false;
		this.countdownTimer.Interval = 1000;
		this.countdownTimer.Tick += new System.EventHandler(countdownTimer_Tick);
		this.tasksHeaderPanel.BackColor = System.Drawing.Color.Transparent;
		this.tasksHeaderPanel.Controls.Add(this.lblTasks);
		this.tasksHeaderPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.tasksHeaderPanel.Location = new System.Drawing.Point(0, 365);
		this.tasksHeaderPanel.Name = "tasksHeaderPanel";
		this.tasksHeaderPanel.Size = new System.Drawing.Size(260, 25);
		this.tasksHeaderPanel.TabIndex = 2;
		this.lblTasks.Dock = System.Windows.Forms.DockStyle.Fill;
		this.lblTasks.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
		this.lblTasks.Location = new System.Drawing.Point(0, 0);
		this.lblTasks.Name = "lblTasks";
		this.lblTasks.Size = new System.Drawing.Size(260, 25);
		this.lblTasks.TabIndex = 1;
		this.lblTasks.Text = "TIMER";
		this.lblTasks.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.toolsHeaderPanel.BackColor = System.Drawing.Color.Transparent;
		this.toolsHeaderPanel.Controls.Add(this.lblTools);
		this.toolsHeaderPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.toolsHeaderPanel.Location = new System.Drawing.Point(0, 340);
		this.toolsHeaderPanel.Name = "toolsHeaderPanel";
		this.toolsHeaderPanel.Size = new System.Drawing.Size(260, 25);
		this.toolsHeaderPanel.TabIndex = 9;
		this.lblTools.Dock = System.Windows.Forms.DockStyle.Fill;
		this.lblTools.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
		this.lblTools.Location = new System.Drawing.Point(0, 0);
		this.lblTools.Name = "lblTools";
		this.lblTools.Size = new System.Drawing.Size(260, 25);
		this.lblTools.TabIndex = 1;
		this.lblTools.Text = "TOOLS";
		this.lblTools.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.toolsRow.BackColor = System.Drawing.Color.Transparent;
		this.toolsRow.Dock = System.Windows.Forms.DockStyle.Top;
		this.toolsRow.Height = 28;
		this.toolsRow.Name = "toolsRow";
		this.toolsRow.TabIndex = 10;
		this.cassettesHeaderPanel.BackColor = System.Drawing.Color.Transparent;
		this.cassettesHeaderPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.cassettesHeaderPanel.Location = new System.Drawing.Point(0, 200);
		this.cassettesHeaderPanel.Name = "cassettesHeaderPanel";
		this.cassettesHeaderPanel.Size = new System.Drawing.Size(260, 25);
		this.cassettesHeaderPanel.TabIndex = 4;

		this.lblCassettes.AutoSize = true;
		this.lblCassettes.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
		this.lblCassettes.Name = "lblCassettes";
		this.lblCassettes.Size = new System.Drawing.Size(260, 25);
		this.lblCassettes.Text = "CASSETTES";
		this.lblCassettes.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblCassettes.BackColor = System.Drawing.Color.Transparent;

		this.lblCassetteCount.AutoSize = true;
		this.lblCassetteCount.Font = new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold);
		this.lblCassetteCount.Name = "lblCassetteCount";
		this.lblCassetteCount.Text = "0/0";
		this.lblCassetteCount.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblCassetteCount.BackColor = System.Drawing.Color.Transparent;
		this.lblCassetteCount.Padding = new System.Windows.Forms.Padding(2, 0, 2, 0);

		this.txtCassetteNum = new TextBox();
		this.txtCassetteNum.BackColor = System.Drawing.Color.FromArgb(235, 235, 235);
		this.txtCassetteNum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.txtCassetteNum.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
		this.txtCassetteNum.ForeColor = System.Drawing.Color.Black;
		this.txtCassetteNum.Location = new System.Drawing.Point(70, 3);
		this.txtCassetteNum.Name = "txtCassetteNum";
		this.txtCassetteNum.Size = new System.Drawing.Size(20, 18);
		this.txtCassetteNum.TabIndex = 1;
		this.txtCassetteNum.Text = "1";
		this.txtCassetteNum.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;

		this.lblCassetteTotal = new Label();
		this.lblCassetteTotal.AutoSize = true;
		this.lblCassetteTotal.Font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
		this.lblCassetteTotal.Location = new System.Drawing.Point(233, 4);
		this.lblCassetteTotal.Name = "lblCassetteTotal";
		this.lblCassetteTotal.Text = "/0";
		this.lblCassetteTotal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

		this.btnCassetteList = new Button();
		this.btnCassetteList.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnCassetteList.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnCassetteList.Font = new System.Drawing.Font("Segoe UI", 5.5f, System.Drawing.FontStyle.Bold);
		this.btnCassetteList.ForeColor = System.Drawing.Color.White;
		this.btnCassetteList.Location = new System.Drawing.Point(145, 0);
		this.btnCassetteList.Name = "btnCassetteList";
		this.btnCassetteList.Size = new System.Drawing.Size(110, 28);
		this.btnCassetteList.TabIndex = 2;
		this.btnCassetteList.Text = "LIST";
		this.btnCassetteList.UseVisualStyleBackColor = false;
		this.btnCassetteList.Click += new EventHandler(this.btnCassetteList_Click);

		this.btnFavList = new Button();
		this.btnFavList.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnFavList.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnFavList.Font = new System.Drawing.Font("Segoe UI", 5.5f, System.Drawing.FontStyle.Bold);
		this.btnFavList.ForeColor = System.Drawing.Color.White;
		this.btnFavList.Location = new System.Drawing.Point(30, 0);
		this.btnFavList.Name = "btnFavList";
		this.btnFavList.Size = new System.Drawing.Size(110, 28);
		this.btnFavList.TabIndex = 3;
		this.btnFavList.Text = "FAV LIST";
		this.btnFavList.UseVisualStyleBackColor = false;
		this.btnFavList.Click += new EventHandler(this.btnFavList_Click);

		this.cassettesHeaderPanel.Controls.Add(this.lblCassetteCount);
		this.cassettesHeaderPanel.Controls.Add(this.lblCassettes);

		this.pnlFullListRow = new Panel();
		this.pnlFullListRow.BackColor = System.Drawing.Color.Transparent;
		this.pnlFullListRow.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.pnlFullListRow.Height = 28;
		this.pnlFullListRow.Name = "pnlFullListRow";
		this.pnlFullListRow.Controls.Add(this.btnFavList);
		this.pnlFullListRow.Controls.Add(this.btnCassetteList);

		this.playerFooterPanel.BackColor = System.Drawing.Color.Transparent;
		this.playerFooterPanel.Controls.Add(this.pnlEqualizer);
		this.playerFooterPanel.Controls.Add(this.pnlVolume);
		this.playerFooterPanel.Controls.Add(this.pnlCassetteContainer);
		this.playerFooterPanel.Controls.Add(this.lblM3uTitle);
		this.playerFooterPanel.Controls.Add(this.lblMetadata);
		this.playerFooterPanel.Controls.Add(this.lblExtraMetadata);
		this.playerFooterPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.playerFooterPanel.Location = new System.Drawing.Point(0, 200);
		this.playerFooterPanel.Name = "playerFooterPanel";
		this.playerFooterPanel.Size = new System.Drawing.Size(260, 300);
		this.playerFooterPanel.TabIndex = 3;
		this.pnlEqualizer.BackColor = System.Drawing.Color.Transparent;
		this.pnlEqualizer.Location = new System.Drawing.Point(28, 93);
		this.pnlEqualizer.Name = "pnlEqualizer";
		this.pnlEqualizer.Size = new System.Drawing.Size(204, 26);
		this.pnlEqualizer.TabIndex = 6;
		this.pnlVolume.Controls.Add(this.btnVolLow);
		this.pnlVolume.Controls.Add(this.btnVolMid);
		this.pnlVolume.Controls.Add(this.btnVolMax);
		this.pnlVolume.Controls.Add(this.btnPlayPlayer);
		this.pnlVolume.Controls.Add(this.btnStopPlayer);
		this.pnlVolume.Controls.Add(this.pnlVolumeLine);
		this.pnlVolume.Controls.Add(this.pnlVolumeThumb);
		this.pnlVolume.Location = new System.Drawing.Point(5, 216);
		this.pnlVolume.Name = "pnlVolume";
		this.pnlVolume.Size = new System.Drawing.Size(250, 60);
		this.pnlVolume.TabIndex = 5;
		this.btnVolLow.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnVolLow.Font = new System.Drawing.Font("Segoe UI", 5.5f, System.Drawing.FontStyle.Underline | System.Drawing.FontStyle.Bold);
		this.btnVolLow.ForeColor = System.Drawing.Color.White;
		this.btnVolLow.BackColor = System.Drawing.Color.FromArgb(0, 0, 0, 0);
		this.btnVolLow.Location = new System.Drawing.Point(5, 3);
		this.btnVolLow.Name = "btnVolLow";
		this.btnVolLow.Size = new System.Drawing.Size(28, 22);
		this.btnVolLow.Text = "LOW";
		this.btnVolMid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnVolMid.Font = new System.Drawing.Font("Segoe UI", 5.5f, System.Drawing.FontStyle.Bold);
		this.btnVolMid.ForeColor = System.Drawing.Color.White;
		this.btnVolMid.BackColor = System.Drawing.Color.FromArgb(0, 0, 0, 0);
		this.btnVolMid.Location = new System.Drawing.Point(35, 3);
		this.btnVolMid.Name = "btnVolMid";
		this.btnVolMid.Size = new System.Drawing.Size(28, 22);
		this.btnVolMid.Text = "MID";
		this.btnVolMax.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnVolMax.Font = new System.Drawing.Font("Segoe UI", 5.5f, System.Drawing.FontStyle.Bold);
		this.btnVolMax.ForeColor = System.Drawing.Color.White;
		this.btnVolMax.BackColor = System.Drawing.Color.FromArgb(0, 0, 0, 0);
		this.btnVolMax.Location = new System.Drawing.Point(65, 3);
		this.btnVolMax.Name = "btnVolMax";
		this.btnVolMax.Size = new System.Drawing.Size(28, 22);
		this.btnVolMax.Text = "MAX";
		this.btnPlayPlayer.FlatAppearance.BorderSize = 0;
		this.btnPlayPlayer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnPlayPlayer.Font = new System.Drawing.Font("Segoe UI", 5.5f, System.Drawing.FontStyle.Bold);
		this.btnPlayPlayer.BackColor = System.Drawing.Color.FromArgb(0, 0, 0, 0);
		this.btnPlayPlayer.ForeColor = System.Drawing.Color.White;
		this.btnPlayPlayer.Location = new System.Drawing.Point(5, 32);
		this.btnPlayPlayer.Name = "btnPlayPlayer";
		this.btnPlayPlayer.Size = new System.Drawing.Size(48, 20);
		this.btnPlayPlayer.TabIndex = 2;
		this.btnPlayPlayer.Text = "PLAY";
		this.btnPlayPlayer.UseVisualStyleBackColor = false;
		this.btnPlayPlayer.Padding = new System.Windows.Forms.Padding(0);
		this.btnStopPlayer.FlatAppearance.BorderSize = 0;
		this.btnStopPlayer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnStopPlayer.Font = new System.Drawing.Font("Segoe UI", 5.5f, System.Drawing.FontStyle.Bold);
		this.btnStopPlayer.BackColor = System.Drawing.Color.FromArgb(0, 0, 0, 0);
		this.btnStopPlayer.ForeColor = System.Drawing.Color.White;
		this.btnStopPlayer.Location = new System.Drawing.Point(53, 32);
		this.btnStopPlayer.Name = "btnStopPlayer";
		this.btnStopPlayer.Size = new System.Drawing.Size(48, 20);
		this.btnStopPlayer.TabIndex = 3;
		this.btnStopPlayer.Text = "STOP";
		this.btnStopPlayer.UseVisualStyleBackColor = false;
		this.btnStopPlayer.Padding = new System.Windows.Forms.Padding(0);
		this.pnlVolumeLine.BackColor = System.Drawing.Color.Gray;
		this.pnlVolumeLine.Controls.Add(this.pnlVolumeThumb);
		this.pnlVolumeLine.Location = new System.Drawing.Point(105, 38);
		this.pnlVolumeLine.Name = "pnlVolumeLine";
		this.pnlVolumeLine.Size = new System.Drawing.Size(140, 4);
		this.pnlVolumeLine.TabIndex = 0;
		this.pnlVolumeThumb.BackColor = System.Drawing.Color.White;
		this.pnlVolumeThumb.Location = new System.Drawing.Point(0, -4);
		this.pnlVolumeThumb.Name = "pnlVolumeThumb";
		this.pnlVolumeThumb.Size = new System.Drawing.Size(12, 12);
		this.pnlVolumeThumb.TabIndex = 0;
		this.pnlCassetteContainer.BackColor = System.Drawing.Color.Transparent;
		this.pnlCassetteContainer.Controls.Add(this.picPlayer);
		this.pnlCassetteContainer.Controls.Add(this.picPlayerNext);
		this.pnlCassetteContainer.Location = new System.Drawing.Point(53, 0);
		this.pnlCassetteContainer.Name = "pnlCassetteContainer";
		this.pnlCassetteContainer.Size = new System.Drawing.Size(154, 99);
		this.pnlCassetteContainer.TabIndex = 1;
		this.picPlayer.BackColor = System.Drawing.Color.Transparent;
		this.picPlayer.Location = new System.Drawing.Point(0, 0);
		this.picPlayer.Name = "picPlayer";
		this.picPlayer.Size = new System.Drawing.Size(154, 97);
		this.picPlayer.TabIndex = 0;
		this.picPlayer.TabStop = false;
		this.picPlayerNext.BackColor = System.Drawing.Color.Transparent;
		this.picPlayerNext.Location = new System.Drawing.Point(154, 0);
		this.picPlayerNext.Name = "picPlayerNext";
		this.picPlayerNext.Size = new System.Drawing.Size(154, 97);
		this.picPlayerNext.TabIndex = 1;
		this.picPlayerNext.TabStop = false;
		this.lblM3uTitle.Font = new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold);
		this.lblM3uTitle.Location = new System.Drawing.Point(10, 124);
		this.lblM3uTitle.Name = "lblM3uTitle";
		this.lblM3uTitle.Size = new System.Drawing.Size(240, 32);
		this.lblM3uTitle.TabIndex = 0;
		this.lblM3uTitle.Text = "M3U TITLE";
		this.lblM3uTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblMetadata.Font = new System.Drawing.Font("Segoe UI", 7f, System.Drawing.FontStyle.Bold);
		this.lblMetadata.Location = new System.Drawing.Point(10, 158);
		this.lblMetadata.Name = "lblMetadata";
		this.lblMetadata.Size = new System.Drawing.Size(240, 22);
		this.lblMetadata.TabIndex = 6;
		this.lblMetadata.Text = "SONG TITLE";
		this.lblMetadata.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblExtraMetadata.Font = new System.Drawing.Font("Segoe UI", 6.5f);
		this.lblExtraMetadata.Location = new System.Drawing.Point(10, 182);
		this.lblExtraMetadata.Name = "lblExtraMetadata";
		this.lblExtraMetadata.Size = new System.Drawing.Size(240, 22);
		this.lblExtraMetadata.TabIndex = 7;
		this.lblExtraMetadata.Text = "ARTIST - ALBUM";
		this.lblExtraMetadata.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.tasksListPanel.AutoScroll = true;
		this.tasksListPanel.BackColor = System.Drawing.Color.FromArgb(255, 12, 12, 15);
		this.tasksListPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tasksListPanel.Location = new System.Drawing.Point(0, 390);
		this.tasksListPanel.Name = "tasksListPanel";
		this.tasksListPanel.Size = new System.Drawing.Size(260, 840);
		this.tasksListPanel.TabIndex = 5;

		base.AutoScaleDimensions = new System.Drawing.SizeF(10f, 25f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(280, 760);
		base.Controls.Add(this.cassettesHeaderPanel);
		base.Controls.Add(this.pnlFullListRow);
		base.Controls.Add(this.playerFooterPanel);
		base.Controls.Add(this.btnCloseApp);
		base.Controls.Add(this.tasksListPanel);
		base.Controls.Add(this.tasksHeaderPanel);
		base.Controls.Add(this.toolsRow);
		base.Controls.Add(this.toolsHeaderPanel);
		base.Controls.Add(this.pnlGrip);
		base.Controls.Add(this.timerPanel);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		base.MaximizeBox = false;
		base.Name = "Form1";
		this.Text = "CINCROSS";
		base.TopMost = true;
		this.timerPanel.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.picOverlay).EndInit();
		((System.ComponentModel.ISupportInitialize)this.picMainDisplay).EndInit();
		this.pnlTimerControls.ResumeLayout(false);
		this.pnlTopButtons.ResumeLayout(false);
		this.pnlTopButtons.PerformLayout();
		this.tasksHeaderPanel.ResumeLayout(false);
		this.playerFooterPanel.ResumeLayout(false);
		this.pnlVolume.ResumeLayout(false);
		this.pnlVolume.PerformLayout();
		this.pnlVolumeLine.ResumeLayout(false);
		this.pnlCassetteContainer.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.picPlayer).EndInit();
		((System.ComponentModel.ISupportInitialize)this.picPlayerNext).EndInit();
		base.ResumeLayout(false);
	}
}