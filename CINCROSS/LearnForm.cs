using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CINCROSS;

public class LearnForm : Form
{
    private readonly Panel _headerPanel;
    private readonly Button _btnStart;
    private readonly Button _btnSalir;
    private readonly PictureBox _picPreview;
    private readonly Label _lblSubjectName;
    private readonly Button _btnSubjectLeft;
    private readonly Button _btnSubjectRight;
    private readonly Button _btnLevelLeft;
    private readonly Button _btnLevelRight;
    private readonly Label _lblLevelName;
    private int _levelIndex;
    private int _subjectIndex;

    // Section buttons
    private readonly Button _btnProgreso;
    private readonly Button _btnBoosters;
    private readonly Button _btnPassword;
    private readonly Button _btnOpciones;

    // Section overlay panels
    private Panel? _panelProgreso;
    private Panel? _panelBoosters;
    private Panel? _panelPassword;
    private Panel? _panelOpciones;

    private readonly Button[] _sideButtons;

    // Info panel (always visible)
    private readonly Label _lblAliasTitle;
    private readonly Label _lblAlias;
    private readonly TextBox _editAlias;
    private string _alias = "Jugador";
    private readonly Label _lblClass;
    private readonly Label _lblNivel;
    private readonly Panel _expBarBg;
    private readonly Panel _expFill;

    private readonly LearningData _data;
    private SubjectData? _selectedSubject;

    private int _avatarChoice; // 0=boy, 1=girl

    // Customization indices (0 = Ninguno)
    private int _fullOutfitIdx;
    private int _petIdx;
    private int _headIdx;
    private int _hairIdx;
    private int _faceIdx;
    private int _bodyIdx;
    private int _accessoriesIdx;
    private int _bgIdx;

    // Color schemes
    private static readonly (string Name, Color Accent, Color Correct, Color Wrong)[] ColorSchemes =
    {
        ("Verde",   Color.FromArgb(88, 204, 2),  Color.FromArgb(30, 144, 255), Color.FromArgb(255, 68, 68)),
        ("Azul",    Color.FromArgb(30, 144, 255), Color.FromArgb(88, 204, 2),  Color.FromArgb(148, 0, 211)),
        ("Naranja", Color.FromArgb(255, 140, 0),  Color.FromArgb(255, 200, 0), Color.FromArgb(148, 0, 211)),
        ("Amarillo",Color.FromArgb(255, 200, 0),  Color.FromArgb(88, 204, 2),  Color.FromArgb(255, 68, 68)),
        ("Violeta", Color.FromArgb(148, 0, 211),  Color.FromArgb(255, 200, 0), Color.FromArgb(255, 68, 68)),
    };
    private int _currentSchemeIndex;
    private int _savedSchemeIndex;

    // Available files per slot (index 0 = "Ninguno/none", populated at runtime)
    private string?[] _fullOutfitFiles = Array.Empty<string?>();
    private string?[] _petFiles = Array.Empty<string?>();
    private string?[] _headFiles = Array.Empty<string?>();
    private string?[] _hairFiles = Array.Empty<string?>();
    private string?[] _faceFiles = Array.Empty<string?>();
    private string?[] _bodyFiles = Array.Empty<string?>();
    private string?[] _accessoriesFiles = Array.Empty<string?>();
    private string?[] _bgFiles = Array.Empty<string?>();

    private Bitmap? _characterComposite;

    // Quiz mode
    private bool _isQuizMode;
    private Panel _quizContentPanel;
    private Label _lblQuizHeader;
    private Label _lblQuizProgress;
    private Label _lblQuizQuestion;
    private Panel _quizAnswerPanel;
    private Label _lblQuizStats;
    private List<Question> _quizQuestions = new();
    private int _quizCurrentIndex;
    private int _quizCorrect;
    private int _quizWrong;
    private int _quizStreak;
    private int _quizMaxStreak;
    private int _quizTotalScore;
    private DateTime _quizStartTime;
    private bool? _lastAnswerCorrect;
    private int _lastPointsDelta;
    private readonly List<AnswerRecord> _quizAnswers = new();
    private bool _isCorrectionMode;
    private int _correctionIndex;
    private bool _xpAwarded;
    private bool _talkMouthOpenState;
    private Image? _talkMouthClosed;
    private Image? _talkMouthOpen;
    private readonly Timer _talkTimer;
    public bool QuizCompleted { get; private set; }
    public int QuizTotalScore => _quizTotalScore;
    public int QuizMinutesStudied => Math.Max(1, (int)(DateTime.Now - _quizStartTime).TotalMinutes);
    public string SelectedSubjectName => _selectedSubject?.Name ?? "MATH";
    public string SelectedAvatarPath => "000_CA.png";
    public int AvatarChoice => _avatarChoice;
    public string? SelectedFullOutfit => GetItem(_fullOutfitFiles, _fullOutfitIdx);
    public string? SelectedPet => GetItem(_petFiles, _petIdx);
    public string? SelectedHead => GetItem(_headFiles, _headIdx);
    public string? SelectedHair => GetItem(_hairFiles, _hairIdx);
    public string? SelectedFace => GetItem(_faceFiles, _faceIdx);
    public string? SelectedBody => GetItem(_bodyFiles, _bodyIdx);
    public string? SelectedAccessories => GetItem(_accessoriesFiles, _accessoriesIdx);
    public string? SelectedBg => GetItem(_bgFiles, _bgIdx);

    private void LoadCustomizationFiles()
    {
        string dir = ImgDir;
        if (!Directory.Exists(dir))
        {
            _fullOutfitFiles = new string?[] { null };
            _petFiles = new string?[] { null };
            _headFiles = new string?[] { null };
            _hairFiles = new string?[] { null };
            _faceFiles = new string?[] { null };
            _bodyFiles = new string?[] { null };
            _accessoriesFiles = new string?[] { null };
            _bgFiles = new string?[] { null };
            return;
        }

        string[] files = Directory.GetFiles(dir);
        _fullOutfitFiles = BuildFileArray(files, "_T.");
        _petFiles = BuildFileArray(files, "_M.");
        _headFiles = BuildFileArray(files, "_CA.");
        _hairFiles = BuildFileArray(files, "_HA.");
        _faceFiles = BuildFileArray(files, "_EX.");
        _bodyFiles = BuildFileArray(files, "_CU.");
        _accessoriesFiles = BuildFileArray(files, "_AC.");
        _bgFiles = BuildFileArray(files, "_F.");
    }

    private static string?[] BuildFileArray(string[] files, string suffix)
    {
        var matching = files
            .Select(Path.GetFileName)
            .Where(f => f != null && f.IndexOf(suffix, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(f => f)
            .Cast<string?>()
            .ToArray();
        return matching.Length > 0 ? matching : new string?[] { null };
    }

    private static string? GetItem(string?[] files, int idx) =>
        idx >= 0 && idx < files.Length ? files[idx] : null;

    private static int IndexOfFile(string?[] files, string prefix)
    {
        for (int i = 0; i < files.Length; i++)
        {
            string? file = files[i];
            if (file != null && file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return 0;
    }

    private static Region RoundedRect(int w, int h, int r)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(0, 0, r, r, 180, 90);
        path.AddArc(w - r, 0, r, r, 270, 90);
        path.AddArc(w - r, h - r, r, r, 0, 90);
        path.AddArc(0, h - r, r, r, 90, 90);
        path.CloseFigure();
        return new Region(path);
    }

    private static string ImgDir => PathHelper.GetImgDir();

    public LearnForm(LearningData data, int initialAvatarChoice = 0)
    {
        _data = data;
        _avatarChoice = initialAvatarChoice;

        LoadCustomizationFiles();

        // Default avatar presets
        _headIdx = IndexOfFile(_headFiles, "000_CA");
        _fullOutfitIdx = IndexOfFile(_fullOutfitFiles, "001_T");
        _hairIdx = IndexOfFile(_hairFiles, "002_HA");
        _faceIdx = IndexOfFile(_faceFiles, "001_EX");

        Text = "";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(220, 825);
        AutoScroll = true;
        BackColor = Color.FromArgb(35, 35, 35);
        ShowInTaskbar = false;
        TopMost = true;

        int y = 0;

        // --- Header ---
        _headerPanel = new Panel
        {
            Height = 35,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(50, 50, 50)
        };
        Label lblHeader = new Label
        {
            Text = "LEARNING",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(88, 204, 2),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _headerPanel.Controls.Add(lblHeader);

        y = 45;

        // --- Avatar Preview ---
        _picPreview = new PictureBox
        {
            Location = new Point(30, y),
            Size = new Size(160, 160),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(45, 45, 45),
            BorderStyle = BorderStyle.None
        };
        // preview will be updated after combo boxes are created

        // Side icon buttons (avatar category cyclers)
        string[] leftIcons = { "01_ICA.png", "02_ICA.png", "03_ICA.png", "04_ICA.png" };
        string[] rightIcons = { "05_ICA.png", "06_ICA.png", "07_ICA.png", "08_ICA.png" };
        int sideBtnW = 30, sideBtnH = 24;
        int sideTop = 45 + 13;
        string imgDir = ImgDir;
        var sideBtns = new Button[8];

        Button MakeSideBtn(Point loc, string iconFile)
        {
            var btn = new Button
            {
                Location = loc,
                Size = new Size(sideBtnW, sideBtnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(45, 45, 45),
                BackgroundImageLayout = ImageLayout.Zoom,
                TabStop = false
            };
            string ip = Path.Combine(imgDir, iconFile);
            if (File.Exists(ip)) btn.BackgroundImage = PathHelper.LoadImage(ip);
            return btn;
        }

        for (int i = 0; i < 4; i++)
        {
            int iy = sideTop + i * (sideBtnH + 13);
            int idxL = i;
            int idxR = i + 4;
            sideBtns[idxL] = MakeSideBtn(new Point(0, iy), leftIcons[i]);
            sideBtns[idxL].Click += (_, _) => CycleCategory(idxL);
            Controls.Add(sideBtns[idxL]);
            sideBtns[idxR] = MakeSideBtn(new Point(190, iy), rightIcons[i]);
            sideBtns[idxR].Click += (_, _) => CycleCategory(idxR);
            Controls.Add(sideBtns[idxR]);
        }
        _sideButtons = sideBtns;

        y += 170;

        // --- Info panel (always visible) ---
        int infoY = y + 2;

        _lblAliasTitle = new Label
        {
            Text = "ALIAS:",
            Location = new Point(12, infoY),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 7f, FontStyle.Bold)
        };
        Controls.Add(_lblAliasTitle);

        _lblAlias = new Label
        {
            Text = _alias,
            Location = new Point(62, infoY),
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _lblAlias.Click += (_, _) => BeginEditAlias();
        Controls.Add(_lblAlias);

        _editAlias = new TextBox
        {
            Text = _alias,
            Location = new Point(62, infoY - 2),
            Width = 138,
            Height = 20,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Visible = false
        };
        _editAlias.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { EndEditAlias(); e.SuppressKeyPress = true; }
            if (e.KeyCode == Keys.Escape) { _editAlias.Text = _alias; EndEditAlias(); e.SuppressKeyPress = true; }
        };
        _editAlias.LostFocus += (_, _) => EndEditAlias();
        Controls.Add(_editAlias);

        infoY += 20;

        _lblClass = new Label
        {
            Text = "CLASE: NOVATO",
            Location = new Point(12, infoY),
            AutoSize = true,
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Segoe UI", 7f, FontStyle.Bold)
        };
        Controls.Add(_lblClass);

        infoY += 16;

        _lblNivel = new Label
        {
            Text = "NIVEL: 1",
            Location = new Point(12, infoY),
            AutoSize = true,
            ForeColor = Color.FromArgb(88, 204, 2),
            Font = new Font("Segoe UI", 7f, FontStyle.Bold)
        };
        Controls.Add(_lblNivel);

        infoY += 18;

        // EXP bar background
        _expBarBg = new Panel
        {
            Location = new Point(12, infoY + 2),
            Size = new Size(196, 12),
            BackColor = Color.FromArgb(40, 40, 40)
        };
        _expBarBg.Region = RoundedRect(196, 12, 6);

        _expFill = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(0, 12),
            BackColor = Color.FromArgb(88, 204, 2)
        };
        _expBarBg.Controls.Add(_expFill);
        Controls.Add(_expBarBg);

        y = infoY + 16;

        // --- Section buttons ---
        int sectionBtnW = 196;
        int sectionBtnH = 30;

        Button MakeSectionBtn(string text)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(12, y),
                Size = new Size(sectionBtnW, sectionBtnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(60, 60, 60) },
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                TabStop = false
            };
            return btn;
        }

        _btnProgreso = MakeSectionBtn("PROGRESO");
        _btnProgreso.Click += (_, _) => ShowPanel(ref _panelProgreso, CreatePanelProgreso);
        Controls.Add(_btnProgreso);
        y += sectionBtnH + 4;

        _btnBoosters = MakeSectionBtn("BOOSTERS");
        _btnBoosters.Click += (_, _) => ShowPanel(ref _panelBoosters, CreatePanelBoosters);
        Controls.Add(_btnBoosters);
        y += sectionBtnH + 4;

        _btnPassword = MakeSectionBtn("PASSWORD");
        _btnPassword.Click += (_, _) => ShowPanel(ref _panelPassword, CreatePanelPassword);
        Controls.Add(_btnPassword);
        y += sectionBtnH + 4;

        _btnOpciones = MakeSectionBtn("OPCIONES");
        _btnOpciones.Click += (_, _) => ShowPanel(ref _panelOpciones, CreatePanelOpciones);
        Controls.Add(_btnOpciones);
        y += sectionBtnH + 10;

        // --- Subject spinner ---
        Label lblSubjectLabel = new Label
        {
            Text = "MATERIA",
            Location = new Point(12, y),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        Controls.Add(lblSubjectLabel);
        y += 20;

        int btnSize = 22;
        int numW = 96;

        _btnSubjectLeft = new Button
        {
            Text = "◀",
            Location = new Point(12, y),
            Size = new Size(btnSize, btnSize),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 7f, FontStyle.Bold),
            TabStop = false
        };
        _btnSubjectLeft.FlatAppearance.BorderSize = 0;

        _lblSubjectName = new Label
        {
            Text = "",
            Location = new Point(12 + btnSize, y),
            Size = new Size(numW, btnSize),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(40, 40, 40),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        _btnSubjectRight = new Button
        {
            Text = "▶",
            Location = new Point(12 + btnSize + numW, y),
            Size = new Size(btnSize, btnSize),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 7f, FontStyle.Bold),
            TabStop = false
        };
        _btnSubjectRight.FlatAppearance.BorderSize = 0;

        _btnSubjectLeft.Click += (_, _) => ChangeSubject(-1);
        _btnSubjectRight.Click += (_, _) => ChangeSubject(1);

        Controls.Add(_btnSubjectLeft);
        Controls.Add(_lblSubjectName);
        Controls.Add(_btnSubjectRight);

        y += 25;

        // --- Level spinner ---
        Label lblLevelLabel = new Label
        {
            Text = "NIVEL",
            Location = new Point(12, y),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        Controls.Add(lblLevelLabel);
        y += 20;

        int lvlBtnSize = 22;
        int lvlNumW = 96;

        _btnLevelLeft = new Button
        {
            Text = "◀",
            Location = new Point(12, y),
            Size = new Size(lvlBtnSize, lvlBtnSize),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 7f, FontStyle.Bold),
            TabStop = false
        };
        _btnLevelLeft.FlatAppearance.BorderSize = 0;

        _lblLevelName = new Label
        {
            Text = "LV 1",
            Location = new Point(12 + lvlBtnSize, y),
            Size = new Size(lvlNumW, lvlBtnSize),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(88, 204, 2),
            BackColor = Color.FromArgb(40, 40, 40),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        _btnLevelRight = new Button
        {
            Text = "▶",
            Location = new Point(12 + lvlBtnSize + lvlNumW, y),
            Size = new Size(lvlBtnSize, lvlBtnSize),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 7f, FontStyle.Bold),
            TabStop = false
        };
        _btnLevelRight.FlatAppearance.BorderSize = 0;

        _btnLevelLeft.Click += (_, _) => ChangeLevel(-1);
        _btnLevelRight.Click += (_, _) => ChangeLevel(1);

        Controls.Add(_btnLevelLeft);
        Controls.Add(_lblLevelName);
        Controls.Add(_btnLevelRight);

        y += 30;

        // --- COMENZAR ---
        _btnStart = new Button
        {
            Text = "▶  COMENZAR",
            Location = new Point(12, y),
            Size = new Size(196, 40),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = Color.FromArgb(88, 204, 2),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        _btnStart.Click += BtnStart_Click;

        y += 48;

        _btnSalir = new Button
        {
            Text = "SALIR",
            Location = new Point(12, y),
            Size = new Size(196, 30),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 3, BorderColor = ColorSchemes[_currentSchemeIndex].Accent },
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        _btnSalir.Click += (_, _) => Close();

        y += 36;

        // --- Add all controls (in z-order, last = top) ---
        Controls.Add(_btnSalir);
        Controls.Add(_btnStart);
        Controls.Add(_picPreview);
        Controls.Add(_headerPanel);

        // Quiz content panel (hidden initially, covers setup controls)
        _quizContentPanel = new Panel
        {
            Location = new Point(0, 288),
            Size = new Size(220, 537),
            BackColor = Color.FromArgb(35, 35, 35),
            Visible = false
        };

        _lblQuizHeader = new Label
        {
            Text = "",
            Height = 24,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = ColorSchemes[_currentSchemeIndex].Accent,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _lblQuizProgress = new Label
        {
            Text = "",
            Height = 18,
            Dock = DockStyle.Top,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 7f),
            TextAlign = ContentAlignment.MiddleCenter
        };

        Panel questionBg = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = Color.FromArgb(40, 40, 40),
            Padding = new Padding(10, 0, 10, 0)
        };

        _lblQuizQuestion = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        questionBg.Controls.Add(_lblQuizQuestion);

        _quizAnswerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(10, 10, 10, 5)
        };

        _lblQuizStats = new Label
        {
            Height = 18,
            Dock = DockStyle.Bottom,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 7f),
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label lblQuizEsc = new Label
        {
            Text = "ESC para salir",
            Height = 14,
            Dock = DockStyle.Bottom,
            ForeColor = Color.FromArgb(60, 60, 60),
            Font = new Font("Segoe UI", 6f),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _quizContentPanel.Controls.Add(lblQuizEsc);
        _quizContentPanel.Controls.Add(_lblQuizStats);
        _quizContentPanel.Controls.Add(_quizAnswerPanel);
        _quizContentPanel.Controls.Add(questionBg);
        _quizContentPanel.Controls.Add(_lblQuizProgress);
        _quizContentPanel.Controls.Add(_lblQuizHeader);

        Controls.Add(_quizContentPanel);

        // Load talking mouth overlays for quiz
        {
            string dir = ImgDir;
            string m1 = Path.Combine(dir, "b_sc001_Q.png");
            string m2 = Path.Combine(dir, "b_sc002_Q.png");
            if (File.Exists(m1)) _talkMouthClosed = PathHelper.LoadImage(m1);
            if (File.Exists(m2)) _talkMouthOpen = PathHelper.LoadImage(m2);
        }
        _talkTimer = new Timer { Interval = 600 };
        _talkTimer.Tick += (_, _) =>
        {
            _talkMouthOpenState = !_talkMouthOpenState;
            _picPreview.Invalidate();
        };

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (_isQuizMode)
                {
                    if (_isCorrectionMode)
                        ShowQuizCorrectionSummary();
                    else
                        EndQuiz();
                }
                else
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            }
        };
        FormClosed += (_, _) =>
        {
            _talkTimer?.Dispose();
            _talkMouthClosed?.Dispose();
            _talkMouthOpen?.Dispose();
            if (_characterComposite != null)
            {
                _picPreview.Paint -= PicPreview_Paint;
                _characterComposite.Dispose();
                _characterComposite = null;
            }
        };

        // Init subject index and select last active
        _subjectIndex = _data.Subjects.FindIndex(s => s.Name == _data.LastActiveSubject);
        if (_subjectIndex < 0) _subjectIndex = 0;
        UpdateSubjectDisplay();

        // Color scheme
        _savedSchemeIndex = Math.Clamp(_data.ColorSchemeIndex, 0, ColorSchemes.Length - 1);
        ApplyLearnColorScheme(_savedSchemeIndex);

        UpdatePreview();
        UpdateInfoPanel();
    }

    private void UpdatePreview()
    {
        string baseFile = "000_CA.png";
        string basePath = Path.Combine(ImgDir, baseFile);
        if (!File.Exists(basePath)) return;

        // Clean up previous animated-bg paint hook
        if (_characterComposite != null)
        {
            _picPreview.Paint -= PicPreview_Paint;
            _characterComposite.Dispose();
            _characterComposite = null;
        }

        // Stop any animation and release old image
        Image? oldImg = _picPreview.Image;
        _picPreview.Image = null;
        _picPreview.ImageLocation = null;
        oldImg?.Dispose();

        // Collect overlays in z-order (first = bottom, last = top)
        List<string> overlays = new List<string>(6);
        void AddOverlayLocal(string? file)
        {
            if (!string.IsNullOrEmpty(file))
                overlays.Add(file);
        }
        AddOverlayLocal(SelectedBody);        // CUERPO
        AddOverlayLocal(SelectedHead);        // CABEZA
        AddOverlayLocal(SelectedHair);        // CABELLO
        AddOverlayLocal(SelectedFace);        // EXPRESION (sobre cabello)
        AddOverlayLocal(SelectedFullOutfit);  // TRAJE
        AddOverlayLocal(SelectedAccessories); // ACCESORIO
        AddOverlayLocal(SelectedPet);         // MASCOTA (top)

        string? bgFile = SelectedBg;

        // Composite character (base + all overlays)
        using Image baseImg = PathHelper.LoadImage(basePath);
        int w = baseImg.Width;
        int h = baseImg.Height;
        Bitmap composite = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(composite))
        {
            g.DrawImage(baseImg, 0, 0, w, h);
            foreach (string file in overlays)
            {
                string ovPath = Path.Combine(ImgDir, file);
                if (!File.Exists(ovPath)) continue;
                using Image overlay = PathHelper.LoadImage(ovPath);
                g.DrawImage(overlay, 0, 0, w, h);
            }
        }

        if (bgFile != null)
        {
            string bgPath = Path.Combine(ImgDir, bgFile);
            if (File.Exists(bgPath))
            {
                // Background is animated GIF — load directly to preserve animation
                _characterComposite = composite;
                _picPreview.Image = PathHelper.LoadImage(bgPath);
                _picPreview.Paint += PicPreview_Paint;
                return;
            }
        }

        // No background — show composite directly
        _picPreview.Image = composite;
    }

    private void PicPreview_Paint(object? sender, PaintEventArgs e)
    {
        if (_characterComposite != null)
            e.Graphics.DrawImage(_characterComposite, 0, 0,
                _characterComposite.Width, _characterComposite.Height);

        if (_isQuizMode)
        {
            var mouth = _talkMouthOpenState ? _talkMouthOpen : _talkMouthClosed;
            if (mouth != null)
            {
                int w = _characterComposite?.Width ?? mouth.Width;
                int h = _characterComposite?.Height ?? mouth.Height;
                e.Graphics.DrawImage(mouth, 0, 0, w, h);
            }
        }
    }

    private void ChangeSubject(int delta)
    {
        if (_data.Subjects.Count == 0) return;
        _subjectIndex = (_subjectIndex + delta + _data.Subjects.Count) % _data.Subjects.Count;
        UpdateSubjectDisplay();
        UpdateInfoPanel();
    }

    private void BeginEditAlias()
    {
        _lblAlias.Visible = false;
        _editAlias.Text = _alias;
        _editAlias.Visible = true;
        _editAlias.Focus();
        _editAlias.SelectAll();
    }

    private void EndEditAlias()
    {
        _alias = _editAlias.Text.Trim();
        if (string.IsNullOrEmpty(_alias)) _alias = "Jugador";
        _lblAlias.Text = _alias;
        _editAlias.Visible = false;
        _lblAlias.Visible = true;
    }

    private void UpdateSubjectDisplay()
    {
        if (_subjectIndex < 0 || _subjectIndex >= _data.Subjects.Count) return;
        _selectedSubject = _data.Subjects[_subjectIndex];
        _lblSubjectName.Text = _selectedSubject.DisplayName;
        _lblSubjectName.ForeColor = ColorTranslator.FromHtml(_selectedSubject.ColorHex);
        _data.LastActiveSubject = _selectedSubject.Name;
    }

    private void ChangeLevel(int delta)
    {
        _levelIndex = Math.Clamp(_levelIndex + delta, 0, 2);
        UpdateLevelDisplay();
    }

    private void UpdateLevelDisplay()
    {
        _lblLevelName.Text = $"LV {_levelIndex + 1}";
    }

    private void CycleCategory(int btnIdx)
    {
        switch (btnIdx)
        {
            case 0: _headIdx = (_headIdx + 1) % _headFiles.Length; break;
            case 1: _bodyIdx = (_bodyIdx + 1) % _bodyFiles.Length; break;
            case 2: _fullOutfitIdx = (_fullOutfitIdx + 1) % _fullOutfitFiles.Length; break;
            case 3: _petIdx = (_petIdx + 1) % _petFiles.Length; break;
            case 4: _faceIdx = (_faceIdx + 1) % _faceFiles.Length; break;
            case 5: _hairIdx = (_hairIdx + 1) % _hairFiles.Length; break;
            case 6: _accessoriesIdx = (_accessoriesIdx + 1) % _accessoriesFiles.Length; break;
            case 7: _bgIdx = (_bgIdx + 1) % _bgFiles.Length; break;
        }
        UpdatePreview();
    }

    private void ApplyLearnColorScheme(int index)
    {
        _currentSchemeIndex = index;
        var scheme = ColorSchemes[index];
        Color accent = scheme.Accent;
        _lblNivel.ForeColor = accent;
        _lblLevelName.ForeColor = accent;
        _expFill.BackColor = accent;
        _btnStart.BackColor = accent;
        // Update header label inside _headerPanel
        foreach (Control c in _headerPanel.Controls)
            if (c is Label lbl)
                lbl.ForeColor = accent;
        // Refresh section panel header labels and SALIR borders (if panels exist)
        Panel?[] sectionPanels = { _panelProgreso, _panelBoosters, _panelPassword, _panelOpciones };
        foreach (Panel? p in sectionPanels)
        {
            if (p == null) continue;
            foreach (Control c in p.Controls)
            {
                if (c is Label lblH && lblH.Dock == DockStyle.Top)
                    lblH.ForeColor = accent;
                if (c is Button b && b.Text == "SALIR")
                    b.FlatAppearance.BorderColor = accent;
            }
        }
        // Update main SALIR button border
        _btnSalir.FlatAppearance.BorderColor = accent;
        // Update side avatar-category buttons
        if (_sideButtons != null)
            foreach (Button b in _sideButtons)
                b.BackColor = accent;
    }

    // --- Section overlay panels ---

    private void ShowPanel(ref Panel? panel, Func<Panel> factory)
    {
        if (panel == null)
        {
            panel = factory();
            Controls.Add(panel);
        }
        panel.Visible = true;
        panel.BringToFront();
    }

    private void HidePanel(ref Panel? panel)
    {
        if (panel != null)
            panel.Visible = false;
    }

    private Panel CreateBasePanel(string title)
    {
        Panel p = new Panel
        {
            Location = new Point(0, 288),
            Size = new Size(220, 537),
            BackColor = Color.FromArgb(35, 35, 35),
            AutoScroll = true,
            Visible = true
        };

        Label lblHeader = new Label
        {
            Text = title,
            Height = 24,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = ColorSchemes[_currentSchemeIndex].Accent,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        p.Controls.Add(lblHeader);

        return p;
    }

    private void AddSalirButton(Panel p, int yPos)
    {
        Button btnSalir = new Button
        {
            Text = "SALIR",
            Location = new Point(10, yPos),
            Size = new Size(200, 30),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 3, BorderColor = ColorSchemes[_currentSchemeIndex].Accent },
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            TabStop = false
        };
        btnSalir.Click += (_, _) => p.Visible = false;
        p.Controls.Add(btnSalir);
    }

    private Panel CreatePanelProgreso()
    {
        Panel p = CreateBasePanel("PROGRESO");
        int py = 35;

        Label lblAutosave = new Label
        {
            Text = "AUTOGUARDADO",
            Location = new Point(10, py),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        p.Controls.Add(lblAutosave);
        py += 22;

        CheckBox chkAutosave = new CheckBox
        {
            Text = "Habilitado",
            Location = new Point(10, py),
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8f),
            Checked = true
        };
        p.Controls.Add(chkAutosave);
        py += 30;

        Label lblSlots = new Label
        {
            Text = "SLOTS",
            Location = new Point(10, py),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        p.Controls.Add(lblSlots);
        py += 22;

        string[] slotLabels = { "SLOT 1", "SLOT 2", "SLOT 3" };
        for (int si = 0; si < slotLabels.Length; si++)
        {
            bool isSlot1 = si == 0;
            Button btnSlot = new Button
            {
                Text = isSlot1 ? "SLOT 1 «" : slotLabels[si],
                Location = new Point(10, py),
                Size = new Size(200, 28),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = isSlot1 ? ColorSchemes[_currentSchemeIndex].Accent : Color.FromArgb(60, 60, 60) },
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                TabStop = false
            };
            p.Controls.Add(btnSlot);
            py += 34;
        }

        AddSalirButton(p, py);
        return p;
    }

    private Panel CreatePanelPersonalizar()
    {
        Panel p = CreateBasePanel("PERSONALIZAR");
        int py = 35;
        int colW = 100;

        // --- Edit character spinners ---
        Label lblEdit = new Label
        {
            Text = "EDITAR PERSONAJE:",
            Location = new Point(10, py),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 7f, FontStyle.Bold)
        };
        p.Controls.Add(lblEdit);
        py += 20;

        AddSpinnerRowToPanel(p, "TRAJE", "MASCOTA", ref py, colW,
            _fullOutfitFiles, _fullOutfitIdx, v => { _fullOutfitIdx = v; UpdatePreview(); },
            _petFiles, _petIdx, v => { _petIdx = v; UpdatePreview(); });

        AddSpinnerRowToPanel(p, "CABEZA", "CABELLO", ref py, colW,
            _headFiles, _headIdx, v => { _headIdx = v; UpdatePreview(); },
            _hairFiles, _hairIdx, v => { _hairIdx = v; UpdatePreview(); });

        AddSpinnerRowToPanel(p, "EXPRESION", "CUERPO", ref py, colW,
            _faceFiles, _faceIdx, v => { _faceIdx = v; UpdatePreview(); },
            _bodyFiles, _bodyIdx, v => { _bodyIdx = v; UpdatePreview(); });

        AddSpinnerRowToPanel(p, "ACCESORIO", "FONDO", ref py, colW,
            _accessoriesFiles, _accessoriesIdx, v => { _accessoriesIdx = v; UpdatePreview(); },
            _bgFiles, _bgIdx, v => { _bgIdx = v; UpdatePreview(); });

        py += 10;

        // --- Tienda section ---
        Panel shopP = new Panel
        {
            Location = new Point(10, py),
            Size = new Size(200, 50),
            BackColor = Color.FromArgb(45, 45, 45)
        };
        Label lblShop = new Label
        {
            Text = "TIENDA",
            Location = new Point(8, 4),
            AutoSize = true,
            ForeColor = Color.FromArgb(255, 200, 0),
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        Label lblPlaceholder = new Label
        {
            Text = "Próximamente...",
            Location = new Point(8, 24),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 7f)
        };
        shopP.Controls.Add(lblShop);
        shopP.Controls.Add(lblPlaceholder);
        p.Controls.Add(shopP);
        py += 60;

        AddSalirButton(p, py);
        return p;
    }

    private void AddSpinnerRowToPanel(Panel parent, string label1, string label2, ref int py, int colW,
        string?[] files1, int idx1, Action<int> setter1,
        string?[] files2, int idx2, Action<int> setter2)
    {
        // Left spinner
        Button btnLeft1 = new Button
        {
            Text = "◀",
            Location = new Point(10, py),
            Size = new Size(16, 20),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 6f, FontStyle.Bold),
            TabStop = false
        };
        btnLeft1.FlatAppearance.BorderSize = 0;

        Label lblName1 = new Label
        {
            Text = Path.GetFileNameWithoutExtension(files1[Math.Max(0, idx1)] ?? label1),
            Location = new Point(26, py),
            Size = new Size(colW - 32, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(40, 40, 40),
            Font = new Font("Segoe UI", 6f)
        };

        Button btnRight1 = new Button
        {
            Text = "▶",
            Location = new Point(26 + colW - 32, py),
            Size = new Size(16, 20),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 6f, FontStyle.Bold),
            TabStop = false
        };
        btnRight1.FlatAppearance.BorderSize = 0;

        int cur1 = idx1;
        btnLeft1.Click += (_, _) => { if (cur1 > 0) { cur1--; setter1(cur1); lblName1.Text = Path.GetFileNameWithoutExtension(files1[cur1] ?? label1); } };
        btnRight1.Click += (_, _) => { if (cur1 < files1.Length - 1) { cur1++; setter1(cur1); lblName1.Text = Path.GetFileNameWithoutExtension(files1[cur1] ?? label1); } };

        parent.Controls.Add(btnLeft1);
        parent.Controls.Add(lblName1);
        parent.Controls.Add(btnRight1);

        // Label under left spinner
        Label lblTitle1 = new Label
        {
            Text = label1,
            Location = new Point(10, py + 20),
            Size = new Size(colW, 14),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 6f)
        };
        parent.Controls.Add(lblTitle1);

        // Right spinner
        int rx = 10 + colW;
        Button btnLeft2 = new Button
        {
            Text = "◀",
            Location = new Point(rx, py),
            Size = new Size(16, 20),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 6f, FontStyle.Bold),
            TabStop = false
        };
        btnLeft2.FlatAppearance.BorderSize = 0;

        Label lblName2 = new Label
        {
            Text = Path.GetFileNameWithoutExtension(files2[Math.Max(0, idx2)] ?? label2),
            Location = new Point(rx + 16, py),
            Size = new Size(colW - 32, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(40, 40, 40),
            Font = new Font("Segoe UI", 6f)
        };

        Button btnRight2 = new Button
        {
            Text = "▶",
            Location = new Point(rx + 16 + colW - 32, py),
            Size = new Size(16, 20),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 6f, FontStyle.Bold),
            TabStop = false
        };
        btnRight2.FlatAppearance.BorderSize = 0;

        int cur2 = idx2;
        btnLeft2.Click += (_, _) => { if (cur2 > 0) { cur2--; setter2(cur2); lblName2.Text = Path.GetFileNameWithoutExtension(files2[cur2] ?? label2); } };
        btnRight2.Click += (_, _) => { if (cur2 < files2.Length - 1) { cur2++; setter2(cur2); lblName2.Text = Path.GetFileNameWithoutExtension(files2[cur2] ?? label2); } };

        parent.Controls.Add(btnLeft2);
        parent.Controls.Add(lblName2);
        parent.Controls.Add(btnRight2);

        Label lblTitle2 = new Label
        {
            Text = label2,
            Location = new Point(rx, py + 20),
            Size = new Size(colW, 14),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 6f)
        };
        parent.Controls.Add(lblTitle2);

        py += 36;
    }

    private Panel CreatePanelBoosters()
    {
        Panel p = CreateBasePanel("BOOSTERS");
        int py = 35;

        Label lblPlaceholder = new Label
        {
            Text = "Próximamente...",
            Location = new Point(10, py),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 9f)
        };
        p.Controls.Add(lblPlaceholder);

        AddSalirButton(p, py);
        return p;
    }

    private Panel CreatePanelPassword()
    {
        Panel p = CreateBasePanel("PASSWORD");
        int py = 35;

        Label lblPrompt = new Label
        {
            Text = "INGRESE PASSWORD:",
            Location = new Point(10, py),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        p.Controls.Add(lblPrompt);
        py += 22;

        TextBox txtPassword = new TextBox
        {
            Location = new Point(10, py),
            Size = new Size(200, 22),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9f),
            PasswordChar = '*',
            ReadOnly = true
        };
        txtPassword.Text = "(de momento ninguno)";
        p.Controls.Add(txtPassword);
        py += 30;

        AddSalirButton(p, py);
        return p;
    }

    private Panel CreatePanelOpciones()
    {
        Panel p = CreateBasePanel("OPCIONES");
        int py = 35;

        Label lblColor = new Label
        {
            Text = "COLOR PRINCIPAL",
            Location = new Point(10, py),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        p.Controls.Add(lblColor);
        py += 22;

        for (int i = 0; i < ColorSchemes.Length; i++)
        {
            int ci = i;
            Button btnColor = new Button
            {
                Text = ColorSchemes[i].Name,
                Location = new Point(10, py),
                Size = new Size(200, 28),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = ColorSchemes[i].Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                TabStop = false
            };
            btnColor.Click += (_, _) =>
            {
                ApplyLearnColorScheme(ci);
                _data.ColorSchemeIndex = ci;
                _data.Save();
            };
            p.Controls.Add(btnColor);
            py += 34;
        }

        py += 10;
        AddSalirButton(p, py);
        return p;
    }

    private void UpdateInfoPanel(int extraXp = 0)
    {
        if (_selectedSubject == null) return;
        _lblClass.Text = "CLASE: NOVATO";
        _lblNivel.Text = $"NIVEL: {_selectedSubject.Level}";

        int displayXp = _selectedSubject.TotalXp + extraXp;
        int xpInLevel = displayXp % 1000;
        float pct = xpInLevel / 1000f;
        int fillW = (int)(_expBarBg.Width * Math.Min(1f, pct));
        _expFill.Width = fillW;
        _expFill.Region = fillW > 0 ? RoundedRect(fillW, _expFill.Height, 6) : null;
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_selectedSubject == null) return;
        _data.LastActiveSubject = _selectedSubject.Name;
        EnterQuizMode();
    }

    private void EnterQuizMode()
    {
        _isQuizMode = true;

        // Hide setup controls, keep header + preview + info panel visible
        foreach (Control ctrl in Controls)
        {
            if (ctrl == _headerPanel || ctrl == _picPreview || ctrl == _quizContentPanel
                || ctrl == _lblAliasTitle || ctrl == _lblAlias || ctrl == _editAlias
                || ctrl == _lblClass || ctrl == _lblNivel || ctrl == _expBarBg)
                continue;
            ctrl.Visible = false;
        }

        // Hide any open section panels
        HidePanel(ref _panelProgreso);
        HidePanel(ref _panelBoosters);
        HidePanel(ref _panelPassword);
        HidePanel(ref _panelOpciones);

        _quizContentPanel.Visible = true;

        // Show info panel controls
        _lblAliasTitle.Visible = true;
        _lblAlias.Visible = true;
        _editAlias.Visible = false;
        _lblClass.Visible = true;
        _lblNivel.Visible = true;
        _expBarBg.Visible = true;

        UpdateInfoPanel();

        var levels = QuestionBank.Load(_selectedSubject!.Name);
        if (levels == null || levels.Count == 0)
        {
            _lblQuizQuestion.Text = "No hay preguntas disponibles.";
            return;
        }
        _quizQuestions = QuestionBank.GetRandomQuestions(levels, 15, _levelIndex + 1);
        if (_quizQuestions.Count == 0)
        {
            _lblQuizQuestion.Text = "No hay preguntas disponibles.";
            return;
        }

        _xpAwarded = false;
        _quizCurrentIndex = 0;
        _quizCorrect = 0;
        _quizWrong = 0;
        _quizStreak = 0;
        _quizMaxStreak = 0;
        _quizTotalScore = 0;
        _quizStartTime = DateTime.Now;
        _quizAnswers.Clear();
        _isCorrectionMode = false;
        _lastAnswerCorrect = null;

        ClientSize = new Size(220, 825);

        _talkMouthOpenState = false;
        _picPreview.Paint -= PicPreview_Paint;
        _picPreview.Paint += PicPreview_Paint;
        _talkTimer.Start();

        ShowQuizQuestion();
    }

    private void ShowQuizQuestion()
    {
        _isCorrectionMode = false;
        _quizAnswerPanel.Visible = true;
        _lblQuizStats.Visible = false;

        if (_quizCurrentIndex >= _quizQuestions.Count)
        {
            EndQuiz();
            return;
        }
        var q = _quizQuestions[_quizCurrentIndex];

        string name = _selectedSubject!.Name;
        if (_lastAnswerCorrect == null)
        {
            _lblQuizHeader.ForeColor = Color.White;
            _lblQuizHeader.Text = name;
        }
        else if (_lastAnswerCorrect == true)
        {
            _lblQuizHeader.ForeColor = ColorSchemes[_currentSchemeIndex].Accent;
            _lblQuizHeader.Text = $"{name}⭐ +{_lastPointsDelta}pts";
        }
        else
        {
            _lblQuizHeader.ForeColor = ColorSchemes[_currentSchemeIndex].Wrong;
            _lblQuizHeader.Text = $"{name}⭐ -{Math.Abs(_lastPointsDelta)}pts";
        }

        _lblQuizProgress.Text = $"Pregunta {_quizCurrentIndex + 1} / {_quizQuestions.Count}";
        _lblQuizQuestion.Text = q.Text;

        _quizAnswerPanel.Controls.Clear();

        if (q.Type == "truefalse")
            CreateQuizTrueFalseButtons(q);
        else
            CreateQuizOptionButtons(q);
    }

    private void CreateQuizTrueFalseButtons(Question q)
    {
        Color correctColor = ColorSchemes[_currentSchemeIndex].Correct;
        Color wrongColor = ColorSchemes[_currentSchemeIndex].Wrong;

        Button btnTrue = new Button
        {
            Text = "✔",
            Size = new Size(80, 42),
            Location = new Point(25, 14),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = correctColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold)
        };
        btnTrue.Click += (_, _) => AnswerQuizQuestion(q, true, -1);

        Button btnFalse = new Button
        {
            Text = "✘",
            Size = new Size(80, 42),
            Location = new Point(115, 14),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = wrongColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold)
        };
        btnFalse.Click += (_, _) => AnswerQuizQuestion(q, false, -1);

        _quizAnswerPanel.Controls.Add(btnTrue);
        _quizAnswerPanel.Controls.Add(btnFalse);
    }

    private void CreateQuizOptionButtons(Question q)
    {
        int y = 5;
        int btnH = 35;
        string[] labels = { "A", "B", "C", "D" };

        for (int i = 0; i < q.Options.Count; i++)
        {
            int idx = i;
            Button btn = new Button
            {
                Text = $"{labels[i]}: {q.Options[i]}",
                Width = 200,
                Height = btnH,
                Location = new Point(0, y),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(80, 80, 80) },
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            btn.Click += (_, _) => AnswerQuizQuestion(q, idx == q.CorrectIndex, idx);
            _quizAnswerPanel.Controls.Add(btn);
            y += btnH + 4;
        }
    }

    private void AnswerQuizQuestion(Question q, bool isCorrect, int selectedOption)
    {
        _quizAnswers.Add(new AnswerRecord
        {
            Question = q,
            IsCorrect = isCorrect,
            SelectedOption = selectedOption
        });

        if (isCorrect)
        {
            _quizCorrect++;
            _quizStreak++;
            if (_quizStreak > _quizMaxStreak) _quizMaxStreak = _quizStreak;
            int multiplier = _quizStreak >= 3 ? 3 : 2;
            int earned = q.Points * multiplier / 2;
            _quizTotalScore += earned;
            _lastPointsDelta = earned;
        }
        else
        {
            _quizWrong++;
            _quizStreak = 0;
            _quizTotalScore = Math.Max(0, _quizTotalScore - 5);
            _lastPointsDelta = -5;
        }

        _lastAnswerCorrect = isCorrect;
        _quizCurrentIndex++;
        if (_isQuizMode) UpdateInfoPanel(_quizTotalScore);
        ShowQuizQuestion();
    }

    private void EndQuiz()
    {
        _talkTimer.Stop();
        _quizAnswerPanel.Controls.Clear();
        ShowQuizCorrectionSummary();
    }

    private void AwardQuizXp()
    {
        if (_selectedSubject == null || _xpAwarded || _quizQuestions.Count == 0) return;
        int minutes = Math.Max(1, (int)(DateTime.Now - _quizStartTime).TotalMinutes);
        _selectedSubject.CompleteSession(minutes, _quizTotalScore);
        _data.Save();
        _xpAwarded = true;
        QuizCompleted = true;
    }

    private void ExitQuizMode()
    {
        _talkTimer.Stop();
        _picPreview.Invalidate();

        _quizContentPanel.Visible = false;

        foreach (Control ctrl in Controls)
        {
            if (ctrl == _headerPanel || ctrl == _picPreview || ctrl == _quizContentPanel)
                continue;
            ctrl.Visible = true;
        }

        // Hide any open section panels (after foreach so they don't get shown again)
        HidePanel(ref _panelProgreso);
        HidePanel(ref _panelBoosters);
        HidePanel(ref _panelPassword);
        HidePanel(ref _panelOpciones);

        ClientSize = new Size(220, 825);
        UpdateInfoPanel();
        _isQuizMode = false;
    }

    private void ShowQuizCorrectionSummary()
    {
        _isCorrectionMode = true;
        _correctionIndex = 0;
        _quizAnswerPanel.Visible = true;
        _lblQuizStats.Visible = false;

        _lblQuizHeader.Text = $"{_selectedSubject!.Name}   ✅ {_quizCorrect}   ❌ {_quizWrong}   ⭐ {_quizTotalScore}";
        _lblQuizProgress.Text = _quizMaxStreak >= 3 ? $"🔥 Mejor racha: {_quizMaxStreak}" : "";

        _quizAnswerPanel.Controls.Clear();

        Label lblSummary = new Label
        {
            Text = $"Preguntas: {_quizQuestions.Count}\nCorrectas: {_quizCorrect}\nIncorrectas: {_quizWrong}\nPuntos: {_quizTotalScore}",
            Width = 200,
            Height = 80,
            Location = new Point(10, 5),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _quizAnswerPanel.Controls.Add(lblSummary);

        Button btnReview = new Button
        {
            Text = "VER CORRECCIÓN",
            Size = new Size(180, 35),
            Location = new Point(20, 95),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = ColorSchemes[_currentSchemeIndex].Correct,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        btnReview.Click += (_, _) => ShowQuizCorrection(0);
        _quizAnswerPanel.Controls.Add(btnReview);

        Button btnOk = new Button
        {
            Text = "OK",
            Size = new Size(180, 30),
            Location = new Point(20, 140),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(80, 80, 80) },
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        btnOk.Click += (_, _) =>
        {
            AwardQuizXp();
            ExitQuizMode();
        };
        _quizAnswerPanel.Controls.Add(btnOk);

        _lblQuizQuestion.Text = "Sesión completada";
    }

    private void ShowQuizCorrection(int index)
    {
        if (index < 0 || index >= _quizAnswers.Count) return;
        _correctionIndex = index;
        var rec = _quizAnswers[index];
        var q = rec.Question;
        if (q == null) return;

        _lblQuizHeader.Text = $"Corrección {index + 1}/{_quizAnswers.Count}";
        _lblQuizProgress.Text = rec.IsCorrect ? "✅ Correcta" : "❌ Incorrecta";
        _lblQuizQuestion.Text = q.Text;

        _quizAnswerPanel.Controls.Clear();

        if (q.Type == "truefalse")
        {
            ShowQuizTrueFalseCorrection(q, rec.IsCorrect);
        }
        else
        {
            ShowQuizOptionsCorrection(q, rec);
        }

        int y = _quizAnswerPanel.Height - 60;

        if (index > 0)
        {
            Button btnPrev = new Button
            {
                Text = "◀",
                Size = new Size(40, 30),
                Location = new Point(10, y),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(80, 80, 80) },
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnPrev.Click += (_, _) => ShowQuizCorrection(index - 1);
            _quizAnswerPanel.Controls.Add(btnPrev);
        }

        if (index < _quizAnswers.Count - 1)
        {
            Button btnNext = new Button
            {
                Text = "▶",
                Size = new Size(40, 30),
                Location = new Point(170, y),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(80, 80, 80) },
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnNext.Click += (_, _) => ShowQuizCorrection(index + 1);
            _quizAnswerPanel.Controls.Add(btnNext);
        }

        Button btnBack = new Button
        {
            Text = "VOLVER",
            Size = new Size(80, 30),
            Location = new Point(70, y),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(80, 80, 80) },
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 7f, FontStyle.Bold)
        };
        btnBack.Click += (_, _) => ShowQuizCorrectionSummary();
        _quizAnswerPanel.Controls.Add(btnBack);
    }

    private void ShowQuizTrueFalseCorrection(Question q, bool userCorrect)
    {
        Color correctColor = ColorSchemes[_currentSchemeIndex].Correct;
        Color wrongColor = ColorSchemes[_currentSchemeIndex].Wrong;
        Color neutralColor = Color.FromArgb(50, 50, 50);

        Color trueColor = q.CorrectAnswer ? correctColor : neutralColor;
        Color falseColor = !q.CorrectAnswer ? correctColor : neutralColor;

        Button btnTrue = new Button
        {
            Text = "VERDADERO",
            Size = new Size(90, 45),
            Location = new Point(10, 10),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = trueColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        _quizAnswerPanel.Controls.Add(btnTrue);

        Button btnFalse = new Button
        {
            Text = "FALSO",
            Size = new Size(90, 45),
            Location = new Point(110, 10),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = falseColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        _quizAnswerPanel.Controls.Add(btnFalse);

        Label lblResult = new Label
        {
            Text = userCorrect ? "✅ ¡Correcto!" : $"❌ Respuesta: {(q.CorrectAnswer ? "VERDADERO" : "FALSO")}",
            Width = 200,
            Height = 25,
            Location = new Point(10, 65),
            ForeColor = userCorrect ? correctColor : wrongColor,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _quizAnswerPanel.Controls.Add(lblResult);
    }

    private void ShowQuizOptionsCorrection(Question q, AnswerRecord rec)
    {
        Color correctColor = ColorSchemes[_currentSchemeIndex].Correct;
        Color wrongColor = ColorSchemes[_currentSchemeIndex].Wrong;

        int y = 5;
        int btnH = 35;
        string[] labels = { "A", "B", "C", "D" };

        for (int i = 0; i < q.Options.Count; i++)
        {
            Color bkColor;
            if (i == q.CorrectIndex)
                bkColor = correctColor;
            else if (i == rec.SelectedOption && !rec.IsCorrect)
                bkColor = wrongColor;
            else
                bkColor = Color.FromArgb(45, 45, 45);

            Color fgColor = (i == q.CorrectIndex || (i == rec.SelectedOption && !rec.IsCorrect))
                ? Color.White : Color.LightGray;

            Button btn = new Button
            {
                Text = $"{labels[i]}: {q.Options[i]}",
                Width = 200,
                Height = btnH,
                Location = new Point(0, y),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = bkColor,
                ForeColor = fgColor,
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Enabled = false
            };
            _quizAnswerPanel.Controls.Add(btn);
            y += btnH + 4;
        }

        Label lblResult = new Label
        {
            Text = rec.IsCorrect ? "✅ ¡Correcto!" : "❌ Incorrecta",
            Width = 200,
            Height = 20,
            Location = new Point(10, y + 2),
            ForeColor = rec.IsCorrect ? correctColor : wrongColor,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _quizAnswerPanel.Controls.Add(lblResult);
    }

}
