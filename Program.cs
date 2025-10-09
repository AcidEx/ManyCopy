using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;
using ManyCopy.Core;
using Microsoft.Win32;

namespace ManyCopy
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Classic WinForms init (no ApplicationConfiguration)
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            using (var splash = new SplashForm())
            {
                splash.Show();
                Application.DoEvents(); // let splash paint

                var minSplash = TimeSpan.FromMilliseconds(2000);
                var sw = System.Diagnostics.Stopwatch.StartNew();

                var main = new MainForm();

                // Keep splash up until the minimum time has elapsed
                while (sw.Elapsed < minSplash)
                {
                    Application.DoEvents();                 // keep UI responsive
                    System.Threading.Thread.Sleep(15);      // tiny nap
                }

                splash.Close();
                Application.Run(main);
            }
        }
    }

    // ========= Splash (loads Assets/splash.* if present; otherwise minimal fallback) =========
    public sealed class SplashForm : Form
    {
        private Image? _bg;

        public SplashForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;

            Width = 520;
            Height = 260;
            // Use the same exe icon for splash window
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            TryLoadSplashImage();

            if (_bg != null)
            {
                BackgroundImage = _bg;
                BackgroundImageLayout = ImageLayout.Stretch;
                BackColor = Color.Black;
            }
            else
            {
                BackColor = Color.FromArgb(32, 32, 32);
                Controls.Add(new Label
                {
                    Text = "ManyCopy",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 26f, FontStyle.Bold),
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Left = 28,
                    Top = 38
                });
            }

            Controls.Add(new Label
            {
                Text = GetShortVersionLabel(),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f),
                AutoSize = true,
                BackColor = Color.Transparent,
                Left = 28,
                Top = Height - 72
            });

            Controls.Add(new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Width = Width - 56,
                Height = 14,
                Left = 28,
                Top = Height - 42
            });
        }

        private void TryLoadSplashImage()
        {
            try
            {
                var asm = typeof(SplashForm).Assembly;
                var res = asm.GetManifestResourceNames()
                             .FirstOrDefault(n => n.EndsWith("splash.png", StringComparison.OrdinalIgnoreCase)
                                               || n.EndsWith("splash.jpg", StringComparison.OrdinalIgnoreCase)
                                               || n.EndsWith("splash.webp", StringComparison.OrdinalIgnoreCase));
                if (res != null)
                {
                    using var s = asm.GetManifestResourceStream(res);
                    if (s != null) { _bg = Image.FromStream(s); return; }
                }

                var assets = Path.Combine(AppContext.BaseDirectory, "Assets");
                // Prefer high-DPI asset if present
                float scale = 1f; try { using var g = CreateGraphics(); scale = g.DpiX / 96f; } catch { }
                var candidates = new[] { "Splash@2x.png", "splash@2x.png", "splash.png", "splash.jpg", "splash.webp", "Splash.png", "Splash.jpg", "Splash.webp" };
                foreach (var name in candidates)
                {
                    if (scale < 1.5f && (name?.IndexOf("@2x", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0) continue;
                    var p = Path.Combine(assets, name);
                    if (File.Exists(p)) { _bg = Image.FromFile(p); return; }
                }
            }
            catch { }
        }

        private static string GetShortVersionLabel()
        {
            try
            {
                // Prefer file version (often four parts), fall back to informational
                var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Application.ExecutablePath);
                var fileVer = fvi?.FileVersion;
                if (!string.IsNullOrWhiteSpace(fileVer))
                {
                    // Normalize to vX.Y.Z[.W]
                    var parts = fileVer.Split('.')
                        .Take(4)
                        .ToArray();
                    return "v" + string.Join('.', parts);
                }

                var info = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
                var shortInfo = info.Split('+','-',' ')[0];
                if (Version.TryParse(shortInfo, out var v))
                {
                    // Include revision when available
                    if (v.Revision >= 0)
                        return $"v{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
                    if (v.Build >= 0)
                        return $"v{v.Major}.{v.Minor}.{v.Build}";
                    return $"v{v.Major}.{v.Minor}";
                }
                return "v" + shortInfo;
            }
            catch { return string.Empty; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _bg?.Dispose();
            base.Dispose(disposing);
        }
    }

    // =================== Theme ===================
    public enum ThemeMode { Light, Dark, Auto }
    public enum AccentPreset { Default, Blue, Green, Purple, Orange, Red }

    public static class Theme
    {
        public static ThemeMode Current { get; private set; } = ThemeMode.Auto;
        public static AccentPreset CurrentAccent { get; private set; } = AccentPreset.Default;

        public sealed class Palette
        {
            public Color Bg = Color.White;
            public Color Panel = Color.White;
            public Color Text = Color.Black;
            public Color Accent = Color.FromArgb(0, 120, 215);
            public Color InputBg = Color.White;
            public Color InputBorder = Color.FromArgb(200, 200, 200);
            public Color LogBg = Color.White;

            public Color ButtonBg = SystemColors.Control;
            public Color ButtonText = Color.Black;
            public Color ButtonBorder = Color.FromArgb(180, 180, 180);
            public Color ButtonBgHover = SystemColors.ControlLight;
            public Color ButtonBgDown = SystemColors.ControlDark;
            public Color DisabledText = Color.FromArgb(140, 140, 140);

            public Color PrimaryBg = Color.FromArgb(0, 120, 215);
            public Color PrimaryText = Color.White;
            public Color PrimaryHover = Color.FromArgb(10, 140, 235);
            public Color PrimaryDown = Color.FromArgb(0, 95, 175);
            public Color PrimaryBorder = Color.FromArgb(0, 95, 175);
        }

        public static ThemeMode DetectSystemTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int v)
                    return v == 0 ? ThemeMode.Dark : ThemeMode.Light;
            }
            catch { }
            return ThemeMode.Light;
        }

        private static Color AccentToColor(AccentPreset preset)
        {
            return preset switch
            {
                AccentPreset.Green => Color.FromArgb(16, 124, 16),
                AccentPreset.Purple => Color.FromArgb(136, 84, 208),
                AccentPreset.Orange => Color.FromArgb(218, 112, 0),
                AccentPreset.Red => Color.FromArgb(210, 22, 22),
                _ => Color.FromArgb(0, 120, 215), // Blue default
            };
        }

        private static Color ChangeBrightness(Color c, int delta)
        {
            int clamp(int v) => Math.Min(255, Math.Max(0, v));
            return Color.FromArgb(
                c.A,
                clamp(c.R + delta),
                clamp(c.G + delta),
                clamp(c.B + delta));
        }

        private static Color Blend(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            byte lerp(byte x, byte y) => (byte)(x + (y - x) * t);
            return Color.FromArgb(
                255,
                lerp(a.R, b.R),
                lerp(a.G, b.G),
                lerp(a.B, b.B));
        }

        public static void SetAccent(AccentPreset preset)
        {
            CurrentAccent = preset;
        }

        public static Palette Resolve(ThemeMode mode)
        {
            if (mode == ThemeMode.Auto) mode = DetectSystemTheme();
            bool noTint = CurrentAccent == AccentPreset.Default;
            var accent = AccentToColor(noTint ? AccentPreset.Blue : CurrentAccent);
            if (mode == ThemeMode.Dark)
            {
                if (noTint)
                {
                    // Original dark palette (no tint), primary uses blue accent
                    return new Palette
                    {
                        Bg = Color.FromArgb(32, 32, 32),
                        Panel = Color.FromArgb(40, 40, 40),
                        Text = Color.Gainsboro,
                        Accent = accent,
                        InputBg = Color.FromArgb(28, 28, 28),
                        InputBorder = Color.FromArgb(70, 70, 70),
                        LogBg = Color.FromArgb(20, 20, 20),
                        ButtonBg = Color.FromArgb(60, 60, 60),
                        ButtonText = Color.Gainsboro,
                        ButtonBorder = Color.FromArgb(90, 90, 90),
                        ButtonBgHover = Color.FromArgb(72, 72, 72),
                        ButtonBgDown = Color.FromArgb(52, 52, 52),
                        DisabledText = Color.FromArgb(110, 110, 110),
                        PrimaryBg = accent,
                        PrimaryText = Color.White,
                        PrimaryHover = ChangeBrightness(accent, 20),
                        PrimaryDown = ChangeBrightness(accent, -25),
                        PrimaryBorder = ChangeBrightness(accent, -25),
                    };
                }
                return new Palette
                {
                    // Tint the dark base with accent so the whole window hue changes
                    Bg = Blend(Color.FromArgb(28, 28, 28), accent, 0.18),
                    Panel = Blend(Color.FromArgb(38, 38, 38), accent, 0.24),
                    Text = Color.Gainsboro,
                    Accent = accent,
                    InputBg = Blend(Color.FromArgb(30, 30, 30), accent, 0.18),
                    InputBorder = Blend(Color.FromArgb(70, 70, 70), accent, 0.15),
                    LogBg = Blend(Color.FromArgb(22, 22, 22), accent, 0.18),

                    ButtonBg = Blend(Color.FromArgb(58, 58, 58), accent, 0.20),
                    ButtonText = Color.Gainsboro,
                    ButtonBorder = Blend(Color.FromArgb(90, 90, 90), accent, 0.15),
                    ButtonBgHover = Blend(Color.FromArgb(72, 72, 72), accent, 0.20),
                    ButtonBgDown = Blend(Color.FromArgb(52, 52, 52), accent, 0.20),
                    DisabledText = Color.FromArgb(110, 110, 110),

                    PrimaryBg = accent,
                    PrimaryText = Color.White,
                    PrimaryHover = ChangeBrightness(accent, 20),
                    PrimaryDown = ChangeBrightness(accent, -25),
                    PrimaryBorder = ChangeBrightness(accent, -25),
                };
            }
            if (noTint)
            {
                // Original light palette (no tint) with blue primary accent
                return new Palette
                {
                    Bg = Color.White,
                    Panel = Color.White,
                    Text = Color.Black,
                    Accent = accent,
                    InputBg = Color.White,
                    InputBorder = Color.FromArgb(200, 200, 200),
                    LogBg = Color.White,
                    ButtonBg = SystemColors.Control,
                    ButtonText = Color.Black,
                    ButtonBorder = Color.FromArgb(180, 180, 180),
                    ButtonBgHover = SystemColors.ControlLight,
                    ButtonBgDown = SystemColors.ControlDark,
                    DisabledText = Color.FromArgb(140, 140, 140),
                    PrimaryBg = accent,
                    PrimaryText = Color.White,
                    PrimaryHover = ChangeBrightness(accent, 20),
                    PrimaryDown = ChangeBrightness(accent, -25),
                    PrimaryBorder = ChangeBrightness(accent, -25),
                };
            }
            var p = new Palette
            {
                // Light theme: bold tinting from accent (kept readable)
                Bg = Blend(Color.White, accent, 0.32),
                Panel = Blend(Color.White, accent, 0.40),
                Text = Color.Black,
                Accent = accent,
                InputBg = Blend(Color.White, accent, 0.28),
                InputBorder = Blend(Color.FromArgb(200,200,200), accent, 0.24),
                LogBg = Blend(Color.White, accent, 0.22),

                ButtonBg = Blend(SystemColors.Control, accent, 0.36),
                ButtonText = Color.Black,
                ButtonBorder = Blend(Color.FromArgb(180,180,180), accent, 0.30),
                ButtonBgHover = Blend(SystemColors.ControlLight, accent, 0.38),
                ButtonBgDown = Blend(SystemColors.ControlDark, accent, 0.34),
                DisabledText = Color.FromArgb(140,140,140),

                PrimaryBg = accent,
                PrimaryText = Color.White,
                PrimaryHover = ChangeBrightness(accent, 20),
                PrimaryDown = ChangeBrightness(accent, -25),
                PrimaryBorder = ChangeBrightness(accent, -25),
            };
            return p;
        }

        public static void ApplyTo(Control root, ThemeMode mode)
        {
            Current = mode;
            var p = Resolve(mode);
            ApplyRecursive(root, p);
        }

        private static void ApplyRecursive(Control c, Palette p)
        {
            switch (c)
            {
                case Form:
                case GroupBox:
                case Panel:
                    c.BackColor = p.Panel; c.ForeColor = p.Text; break;

                case TextBox tb:
                    tb.BackColor = p.InputBg; tb.ForeColor = p.Text; tb.BorderStyle = BorderStyle.FixedSingle; break;

                case ListBox lb:
                    lb.BackColor = p.InputBg; lb.ForeColor = p.Text; lb.BorderStyle = BorderStyle.FixedSingle; break;

                case Label lbl:
                    lbl.BackColor = Color.Transparent; lbl.ForeColor = p.Text; break;

                case Button btn:
                    bool dark = (Current == ThemeMode.Dark) ||
                                (Current == ThemeMode.Auto && DetectSystemTheme() == ThemeMode.Dark);

                    bool primary = (btn.Tag as string) == "primary";
                    if (primary)
                    {
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.UseVisualStyleBackColor = false;
                        btn.Font = new Font(btn.Font, FontStyle.Bold);
                        btn.BackColor = p.PrimaryBg;
                        btn.ForeColor = btn.Enabled ? p.PrimaryText : p.DisabledText;
                        btn.FlatAppearance.BorderSize = 1;
                        btn.FlatAppearance.BorderColor = p.PrimaryBorder;
                        btn.FlatAppearance.MouseOverBackColor = p.PrimaryHover;
                        btn.FlatAppearance.MouseDownBackColor = p.PrimaryDown;

                        btn.EnabledChanged -= PrimaryEnabledChanged;
                        btn.EnabledChanged += PrimaryEnabledChanged;
                        void PrimaryEnabledChanged(object? _, EventArgs __)
                        {
                            btn.ForeColor = btn.Enabled ? p.PrimaryText : p.DisabledText;
                            btn.BackColor = btn.Enabled ? p.PrimaryBg : p.ButtonBg;
                            btn.FlatAppearance.BorderColor = btn.Enabled ? p.PrimaryBorder : p.ButtonBorder;
                        }
                        break;
                    }

                    if (dark)
                    {
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.UseVisualStyleBackColor = false;
                        btn.BackColor = p.ButtonBg;
                        btn.ForeColor = btn.Enabled ? p.ButtonText : p.DisabledText;
                        btn.FlatAppearance.BorderColor = p.ButtonBorder;
                        btn.FlatAppearance.MouseOverBackColor = p.ButtonBgHover;
                        btn.FlatAppearance.MouseDownBackColor = p.ButtonBgDown;
                        btn.FlatAppearance.BorderSize = 1;

                        btn.EnabledChanged -= ButtonEnabledChanged;
                        btn.EnabledChanged += ButtonEnabledChanged;
                        void ButtonEnabledChanged(object? _, EventArgs __)
                        {
                            btn.ForeColor = btn.Enabled ? p.ButtonText : p.DisabledText;
                            btn.BackColor = p.ButtonBg;
                        }
                    }
                    else
                    {
                        btn.FlatStyle = FlatStyle.Standard;
                        btn.UseVisualStyleBackColor = true;
                        btn.ForeColor = p.Text;
                    }
                    break;

                case NumericUpDown nud:
                    nud.BackColor = p.InputBg; nud.ForeColor = p.Text; break;

                case CheckBox:
                case RadioButton:
                    // Avoid the "grey box" look in dark mode: match the parent background
                    c.ForeColor = p.Text;
                    c.BackColor = c.Parent != null ? c.Parent.BackColor : p.Bg;
                    break;

                case ComboBox:
                    // Inputs keep the panel tint for contrast
                    c.ForeColor = p.Text;
                    c.BackColor = p.Panel;
                    break;
            }

            if (c is Form f) f.BackColor = p.Bg;

            foreach (Control child in c.Controls) ApplyRecursive(child, p);
        }
    }

    // =================== Main UI (absolute layout; anchors for scaling) ===================
    public class MainForm : Form
    {
        private ComboBox cmbTheme = null!;
        private ComboBox cmbAccent = null!;
        private Button btnRefreshTheme = null!;

        private TextBox txtSource = null!;
        private readonly List<string> _sources = new();
        private ContextMenuStrip srcMenu = null!;
        private ToolTip srcTip = new ToolTip();
        private Button btnSrcRemove = null!;
        private Button btnSrcClearAll = null!;
        private Button btnBrowseSource = null!;

        private ListBox listDest = null!;
        private readonly HashSet<string> _destSet = new(StringComparer.OrdinalIgnoreCase);
        private Button btnBrowseDest = null!;
        private Button btnRemoveSel = null!;
        private Button btnClear = null!;

                private CheckBox chkOverwrite = null!;
        private CheckBox chkAutoClearDest = null!;
        private CheckBox chkAutoClearSources = null!;
        // Prefix controls (mode dropdown replacing separate checkboxes)
        private ComboBox cmbPrefixMode = null!; // 0=None, 1=Fixed, 2=Numbered
        private TextBox txtPrefix = null!;      // Fixed prefix text
        private TextBox txtPrefixBase = null!;  // Numbered prefix base
        private NumericUpDown nudPrefixStart = null!; // Numbered prefix start
        private NumericUpDown nudPrefixPad = null!;   // Optional zero-padding for numbered prefix
        private ComboBox cmbPrefixSep = null!;        // Separator between prefix and name
        // Suffix controls (mode dropdown with fixed/numbered)
        private ComboBox cmbSuffixMode = null!; // 0=None, 1=Fixed, 2=Numbered
        private TextBox txtSuffix = null!;      // Fixed suffix text
        private TextBox txtSuffixBase = null!;  // Numbered suffix base
        private NumericUpDown nudSuffixStart = null!; // Numbered suffix start
        private NumericUpDown nudSuffixPad = null!;   // Optional zero-padding for numbered suffix
        private ComboBox cmbSuffixSep = null!;        // Separator between name and suffix
        private CheckBox chkPreview = null!;

        private CheckBox chkEnableRange = null!;
        private GroupBox grpRange = null!;
        private TextBox txtRoot = null!;
        private Button btnBrowseRoot = null!;
        private TextBox txtRangePrefix = null!;
        private TextBox txtStart = null!;
        private TextBox txtEnd = null!;
        private CheckBox chkCreateMissing = null!;
        private Button btnAddRange = null!;

        private Button btnEngage = null!;
        private Button btnUndo = null!;
        private Button btnRedo = null!;
        private Label lblStatus = null!;
        private TextBox logBox = null!;

        // Simple profiles
        

        private readonly Stack<HistoryEntry> _undo = new();
        private readonly Stack<HistoryEntry> _redo = new();
        private const int HistoryCap = 100;

        public MainForm()
        {
            SuspendLayout();
            { var info = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty; var shortInfo = info.Split('+','-',' ')[0]; if (Version.TryParse(shortInfo, out var v)) { shortInfo = v.Build >= 0 ? $"{v.Major}.{v.Minor}.{v.Build}" : $"{v.Major}.{v.Minor}"; if (shortInfo.EndsWith(".0")) shortInfo = shortInfo.TrimEnd('0').TrimEnd('.'); } if (string.IsNullOrWhiteSpace(shortInfo)) { shortInfo = typeof(Program).Assembly.GetName().Version?.ToString() ?? string.Empty; } Text = $"ManyCopy v{shortInfo} - Copy Files to Many Folders"; }
            // Use the executable's icon for the window and taskbar
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            Width = 1000;
            Height = 880;
            MinimumSize = new Size(900, 760);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            Font = new Font("Segoe UI", 9f);
            DoubleBuffered = true;

            KeyDown += MainForm_KeyDown;

            // Theme controls
            cmbTheme = new ComboBox { Left = 885, Top = 12, Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cmbTheme.Items.AddRange(new object[] { "Auto (Windows)", "Light", "Dark" });
            cmbTheme.SelectedIndexChanged += (_, __) =>
            {
                var mode = cmbTheme.SelectedIndex switch { 1 => ThemeMode.Light, 2 => ThemeMode.Dark, _ => ThemeMode.Auto };
                Theme.ApplyTo(this, mode);
                SaveTheme(mode);
            };
            Controls.Add(cmbTheme);

            // Accent selector
            cmbAccent = new ComboBox { Left = 790, Top = 12, Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cmbAccent.Items.AddRange(new object[] { "Default", "Blue", "Green", "Purple", "Orange", "Red" });
            cmbAccent.SelectedIndexChanged += (_, __) =>
            {
                var preset = cmbAccent.SelectedIndex switch { 0 => AccentPreset.Default, 1 => AccentPreset.Blue, 2 => AccentPreset.Green, 3 => AccentPreset.Purple, 4 => AccentPreset.Orange, 5 => AccentPreset.Red, _ => AccentPreset.Default };
                Theme.SetAccent(preset);
                var mode = cmbTheme.SelectedIndex switch { 1 => ThemeMode.Light, 2 => ThemeMode.Dark, _ => ThemeMode.Auto };
                Theme.ApplyTo(this, mode);
                // Persist together in the existing settings file format: Mode;Accent
                try { var dir = Path.GetDirectoryName(SettingsPath)!; if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); System.IO.File.WriteAllText(SettingsPath, $"{mode};{preset}"); } catch { }
            };
            Controls.Add(cmbAccent);

            btnRefreshTheme = new Button { Text = "Refresh", Left = 725, Top = 11, Width = 60, Height = 24, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnRefreshTheme.Click += (_, __) =>
            {
                var mode = cmbTheme.SelectedIndex switch { 1 => ThemeMode.Light, 2 => ThemeMode.Dark, _ => ThemeMode.Auto };
                Theme.ApplyTo(this, mode);
            };
            Controls.Add(btnRefreshTheme);

            

            // Source (compact single-line with multi-file support under the hood)
            var lblSource = new Label { Text = "Source files:", Left = 10, Top = 50, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            // Narrow the textbox so it never sits under the new buttons
            txtSource = new TextBox { Left = 95, Top = 47, Width = 620, AllowDrop = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtSource.DragEnter += (s, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true && e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.Any(File.Exists))
                    e.Effect = DragDropEffects.Copy;
            };
            txtSource.DragDrop += (s, e) =>
            {
                if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
                {
                    AddSources(paths.Where(File.Exists));
                }
            };
            // Source action buttons (right-aligned on the same row)
            btnBrowseSource = new Button { Text = "Browse...", Left = 885, Top = 46, Width = 90, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnBrowseSource.Click += (_, __) => PickSources();
            btnSrcClearAll = new Button { Text = "Clear", Left = 804, Top = 46, Width = 75, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnSrcClearAll.Click += (_, __) => { _sources.Clear(); RefreshSourceText(); };
            btnSrcRemove = new Button { Text = "Remove", Left = 728, Top = 46, Width = 70, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnSrcRemove.Click += (_, __) => { BuildSourceMenu(); srcMenu.Show(btnSrcRemove, new System.Drawing.Point(0, btnSrcRemove.Height)); };

            // Context menu for managing multiple sources without changing layout
            srcMenu = new ContextMenuStrip();
            srcMenu.Opening += (s, e) => BuildSourceMenu();
            txtSource.ContextMenuStrip = srcMenu;
            srcTip.SetToolTip(txtSource, string.Empty);
            Controls.AddRange(new Control[] { lblSource, txtSource, btnSrcRemove, btnSrcClearAll, btnBrowseSource });
            // Ensure buttons sit above the textbox in z-order
            try { btnSrcRemove.BringToFront(); btnSrcClearAll.BringToFront(); btnBrowseSource.BringToFront(); } catch { }

            // Range Helper
            chkEnableRange = new CheckBox { Text = "Enable Range Helper", Left = 10, Top = 80, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            chkEnableRange.CheckedChanged += (_, __) => grpRange.Visible = chkEnableRange.Checked;

            grpRange = new GroupBox { Text = "Range Helper", Left = 10, Top = 105, Width = 965, Height = 100, Visible = false, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var lblRoot = new Label { Text = "Root:", Left = 10, Top = 25, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            txtRoot = new TextBox { Left = 60, Top = 22, Width = 800, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseRoot = new Button { Text = "Browse...", Left = 870, Top = 21, Width = 85, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnBrowseRoot.Click += (_, __) => PickRoot();
            var lblRangePrefix = new Label { Text = "Prefix:", Left = 10, Top = 60, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            txtRangePrefix = new TextBox { Left = 60, Top = 57, Width = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            var lblStart = new Label { Text = "Start #:", Left = 230, Top = 60, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            txtStart = new TextBox { Left = 285, Top = 57, Width = 80, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            var lblEnd = new Label { Text = "End #:", Left = 380, Top = 60, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            txtEnd = new TextBox { Left = 430, Top = 57, Width = 80, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            chkCreateMissing = new CheckBox { Text = "Create missing folders", Left = 530, Top = 59, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnAddRange = new Button { Text = "Add Range >>", Left = 760, Top = 56, Width = 195, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnAddRange.Click += (_, __) => AddRangeToList();

            grpRange.Controls.AddRange(new Control[]
            {
                lblRoot, txtRoot, btnBrowseRoot,
                lblRangePrefix, txtRangePrefix, lblStart, txtStart, lblEnd, txtEnd,
                chkCreateMissing, btnAddRange
            });
            Controls.Add(chkEnableRange);
            Controls.Add(grpRange);

            // Destinations
            var lblDest = new Label { Text = "Destination folders:", Left = 10, Top = 235, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            listDest = new ListBox
            {
                Left = 10,
                Top = 255,
                Width = 860,
                Height = 360,
                SelectionMode = SelectionMode.MultiExtended,
                AllowDrop = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            listDest.DragEnter += (s, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true &&
                    e.Data.GetData(DataFormats.FileDrop) is string[] paths &&
                    paths.Any(Directory.Exists))
                    e.Effect = DragDropEffects.Copy;
            };
            listDest.DragDrop += (s, e) =>
            {
                if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
                    foreach (var p in paths.Where(Directory.Exists)) AddDestPath(p);
            };

            btnBrowseDest = new Button { Text = "Browse...", Left = 885, Top = 255, Width = 90, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnBrowseDest.Click += (_, __) => AddMultipleFolders();

            btnRemoveSel = new Button { Text = "Remove", Left = 885, Top = 290, Width = 90, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnRemoveSel.Click += (_, __) => RemoveSelected();

            btnClear = new Button { Text = "Clear All", Left = 885, Top = 325, Width = 90, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnClear.Click += (_, __) => { listDest.Items.Clear(); _destSet.Clear(); };

            Controls.AddRange(new Control[] { lblDest, listDest, btnBrowseDest, btnRemoveSel, btnClear });

            // Options
                        chkOverwrite = new CheckBox { Text = "Overwrite if exists", Left = 10, Top = 650, AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            // Place auto-clear toggles near their related sections
            chkAutoClearSources = new CheckBox { Text = "Auto-clear sources after copy", Left = 10, Top = 26, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            chkAutoClearDest = new CheckBox { Text = "Auto-clear destinations after copy", Left = 10, Top = 215, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left };

            // Prefix mode: None / Fixed / Numbered
            var lblPrefix = new Label { Text = "Prefix:", Left = 160, Top = 650, AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            cmbPrefixMode = new ComboBox { Left = 210, Top = 646, Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            cmbPrefixMode.Items.AddRange(new object[] { "None", "Fixed", "Numbered" });
            cmbPrefixMode.SelectedIndex = 0;

            txtPrefix = new TextBox { Left = 310, Top = 647, Width = 120, Enabled = false, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            txtPrefixBase = new TextBox { Left = 310, Top = 647, Width = 100, Enabled = false, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            var lblStartNum = new Label { Text = "Start:", Left = 415, Top = 650, AutoSize = true, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            nudPrefixStart = new NumericUpDown { Left = 455, Top = 647, Width = 60, Minimum = 0, Maximum = 1_000_000, Value = 1, Enabled = false, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            nudPrefixPad = new NumericUpDown { Left = 520, Top = 647, Width = 50, Minimum = 0, Maximum = 10, Value = 0, Enabled = false, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            cmbPrefixSep = new ComboBox { Left = 575, Top = 646, Width = 60, Enabled = false, Visible = false, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            cmbPrefixSep.Items.AddRange(new object[] { "(none)", "-", "_" }); cmbPrefixSep.SelectedIndex = 0;

            cmbPrefixMode.SelectedIndexChanged += (_, __) =>
            {
                var mode = cmbPrefixMode.SelectedIndex;
                // Reset visibility
                txtPrefix.Visible = (mode == 1); txtPrefix.Enabled = (mode == 1);
                txtPrefixBase.Visible = (mode == 2); txtPrefixBase.Enabled = (mode == 2);
                lblStartNum.Visible = (mode == 2); nudPrefixStart.Visible = (mode == 2); nudPrefixStart.Enabled = (mode == 2);
                nudPrefixPad.Visible = (mode == 2); nudPrefixPad.Enabled = (mode == 2);
                cmbPrefixSep.Visible = (mode != 0); cmbPrefixSep.Enabled = (mode != 0);
            };

            // Suffix mode: None / Fixed / Numbered
            var lblSuffix = new Label { Text = "Suffix:", Left = 530, Top = 650, AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            cmbSuffixMode = new ComboBox { Left = 580, Top = 646, Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            cmbSuffixMode.Items.AddRange(new object[] { "None", "Fixed", "Numbered" });
            cmbSuffixMode.SelectedIndex = 0;

            txtSuffix = new TextBox { Left = 680, Top = 647, Width = 120, Enabled = false, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            txtSuffixBase = new TextBox { Left = 680, Top = 647, Width = 100, Enabled = false, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            var lblSuffixStart = new Label { Text = "Start:", Left = 785, Top = 650, AutoSize = true, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            nudSuffixStart = new NumericUpDown { Left = 825, Top = 647, Width = 60, Minimum = 0, Maximum = 1_000_000, Value = 1, Enabled = false, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            nudSuffixPad = new NumericUpDown { Left = 890, Top = 647, Width = 50, Minimum = 0, Maximum = 10, Value = 0, Enabled = false, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            cmbSuffixSep = new ComboBox { Left = 945, Top = 646, Width = 60, Enabled = false, Visible = false, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            cmbSuffixSep.Items.AddRange(new object[] { "(none)", "-", "_" }); cmbSuffixSep.SelectedIndex = 0;

            cmbSuffixMode.SelectedIndexChanged += (_, __) =>
            {
                var mode = cmbSuffixMode.SelectedIndex;
                txtSuffix.Visible = (mode == 1); txtSuffix.Enabled = (mode == 1);
                txtSuffixBase.Visible = (mode == 2); txtSuffixBase.Enabled = (mode == 2);
                lblSuffixStart.Visible = (mode == 2); nudSuffixStart.Visible = (mode == 2); nudSuffixStart.Enabled = (mode == 2);
                nudSuffixPad.Visible = (mode == 2); nudSuffixPad.Enabled = (mode == 2);
                cmbSuffixSep.Visible = (mode != 0); cmbSuffixSep.Enabled = (mode != 0);
            };

            chkPreview = new CheckBox { Text = "Preview mode", Left = 10, Top = 680, AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };

            Controls.AddRange(new Control[]
            {
                chkOverwrite,
                chkAutoClearDest,
                chkAutoClearSources,
                lblPrefix, cmbPrefixMode, txtPrefix, txtPrefixBase, lblStartNum, nudPrefixStart, nudPrefixPad, cmbPrefixSep,
                lblSuffix, cmbSuffixMode, txtSuffix, txtSuffixBase, lblSuffixStart, nudSuffixStart, nudSuffixPad, cmbSuffixSep,
                chkPreview
            });

            // Actions + log
            btnUndo = new Button { Text = "Undo", Left = 610, Top = 676, Width = 90, Height = 32, Enabled = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            btnUndo.Click += (_, __) => DoUndo();

            btnRedo = new Button { Text = "Redo", Left = 710, Top = 676, Width = 90, Height = 32, Enabled = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            btnRedo.Click += (_, __) => DoRedo();

            btnEngage = new Button { Text = "Engage", Left = 810, Top = 674, Width = 120, Height = 36, Tag = "primary", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            btnEngage.Click += (_, __) => RunCopyOrPreview();

            lblStatus = new Label { Left = 10, Top = 700, AutoSize = true, Text = "Ready", Anchor = AnchorStyles.Bottom | AnchorStyles.Left };

            logBox = new TextBox
            {
                Left = 10,
                Top = 755,
                Width = 965,
                Height = 90,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.AddRange(new Control[] { btnUndo, btnRedo, btnEngage, lblStatus, logBox });
            // Ensure bottom log box stays within window bounds on resize
            Layout += (_, __) => LayoutBottom();
            Resize += (_, __) => LayoutBottom();
            LayoutBottom();
            ResumeLayout(true);

            // Theme + accent on load
            var saved = LoadTheme();
            cmbTheme.SelectedIndex = saved switch { ThemeMode.Light => 1, ThemeMode.Dark => 2, _ => 0 };
            cmbAccent.SelectedIndex = Theme.CurrentAccent switch { AccentPreset.Default => 0, AccentPreset.Blue => 1, AccentPreset.Green => 2, AccentPreset.Purple => 3, AccentPreset.Orange => 4, AccentPreset.Red => 5, _ => 0 };
            Theme.ApplyTo(this, saved);
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z) { DoUndo(); e.Handled = true; }
            else if (e.Control && (e.KeyCode == Keys.Y || (e.Shift && e.KeyCode == Keys.Z))) { DoRedo(); e.Handled = true; }
        }

        private void PickSources()
        {
            using var ofd = new OpenFileDialog { Title = "Select source files", Filter = "All files (*.*)|*.*", Multiselect = true };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                AddSources(ofd.FileNames.Where(File.Exists));
            }
        }

        private void PickRoot()
        {
            using var fbd = new FolderBrowserDialog { Description = "Pick root folder for range" };
            if (fbd.ShowDialog(this) == DialogResult.OK) txtRoot.Text = fbd.SelectedPath;
        }

        private void AddRangeToList()
        {
            var root = (txtRoot.Text ?? string.Empty).Trim();
            var prefix = (txtRangePrefix.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) { Log("ERROR: Destination root invalid."); return; }
            if (string.IsNullOrWhiteSpace(prefix)) { Log("ERROR: Range prefix is empty."); return; }

            var startText = (txtStart.Text ?? string.Empty).Trim();
            var endText = (txtEnd.Text ?? string.Empty).Trim();

            if (!int.TryParse(startText, out var start) ||
                !int.TryParse(endText, out var end)) { Log("ERROR: Start/End must be whole numbers."); return; }
            if (start > end) { Log("ERROR: Start number greater than end number."); return; }

            int padWidth = NamingHelpers.CalculateRangePadWidth(startText, endText);

            int added = 0, created = 0;
            for (int i = start; i <= end; i++)
            {
                string number = NamingHelpers.FormatRangeNumber(i, padWidth);
                var folder = Path.Combine(root, $"{prefix}{number}");
                if (!Directory.Exists(folder))
                {
                    if (chkCreateMissing.Checked)
                    {
                        try { Directory.CreateDirectory(folder); created++; }
                        catch (Exception ex) { Log($"FAILED to create {folder}: {ex.Message}"); continue; }
                    }
                    else { continue; }
                }
                AddDestPath(folder); added++;
            }
            Log($"Range added: {added} folders. Created new folders: {created}");
        }

        private void AddMultipleFolders()
        {
            var paths = ShellFolderPicker.PickMultiple(this);
            if (paths == null || paths.Length == 0) return;
            foreach (var p in paths.Where(Directory.Exists)) AddDestPath(p);
        }

        private void AddDestPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try { path = Path.GetFullPath(path); } catch { }
            if (_destSet.Add(path)) listDest.Items.Add(path);
        }

        private void AddSources(IEnumerable<string> paths)
        {
            foreach (var p in paths)
            {
                try
                {
                    var full = Path.GetFullPath(p);
                    if (File.Exists(full) && !_sources.Contains(full, StringComparer.OrdinalIgnoreCase)) _sources.Add(full);
                }
                catch { }
            }
            _sources.Sort(StringComparer.OrdinalIgnoreCase);
            RefreshSourceText();
        }

        private List<string> GetSelectedSources()
        {
            if (_sources.Count > 0) return new List<string>(_sources);
            var s = (txtSource.Text ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(s) && File.Exists(s)) return new List<string> { s };
            return new List<string>();
        }

        private void RefreshSourceText()
        {
            if (_sources.Count == 0)
            {
                txtSource.Text = string.Empty;
                srcTip.SetToolTip(txtSource, string.Empty);
                return;
            }

            // Compose a single-line display of basenames, trimming if too long
            var names = _sources.Select(Path.GetFileName).ToList();
            string display = ComposeInlineList(names, 120);
            txtSource.Text = display;
            srcTip.SetToolTip(txtSource, string.Join(", ", names));
        }

        private static string ComposeInlineList(IReadOnlyList<string> items, int maxLen)
        {
            if (items.Count == 1) return items[0];
            var parts = new List<string>();
            int len = 0;
            int extra = 0;
            foreach (var it in items)
            {
                int add = (parts.Count > 0 ? 2 : 0) + it.Length; // comma+space + item
                if (len + add > maxLen)
                {
                    extra++;
                    continue;
                }
                parts.Add(it);
                len += add;
            }
            if (extra > 0) parts.Add($"+{extra} more");
            return string.Join(", ", parts);
        }

        private void BuildSourceMenu()
        {
            srcMenu.Items.Clear();
            if (_sources.Count == 0)
            {
                var add = new ToolStripMenuItem("Add Files...");
                add.Click += (_, __) => PickSources();
                srcMenu.Items.Add(add);
                return;
            }

            int shown = 0;
            foreach (var path in _sources)
            {
                if (shown >= 15) { var more = new ToolStripMenuItem("(more items not shown)") { Enabled = false }; srcMenu.Items.Add(more); break; }
                var name = Path.GetFileName(path);
                var item = new ToolStripMenuItem(name) { Tag = path, ToolTipText = path };
                item.Click += (_, __) => { _sources.Remove((string)item.Tag!); RefreshSourceText(); };
                srcMenu.Items.Add(item);
                shown++;
            }

            srcMenu.Items.Add(new ToolStripSeparator());
            var addFiles = new ToolStripMenuItem("Add Files...");
            addFiles.Click += (_, __) => PickSources();
            var clearAll = new ToolStripMenuItem("Clear All");
            clearAll.Click += (_, __) => { _sources.Clear(); RefreshSourceText(); };
            srcMenu.Items.Add(addFiles);
            srcMenu.Items.Add(clearAll);
        }

        private void RemoveSelected()
        {
            var items = listDest.SelectedItems.Cast<object?>().ToList();
            foreach (var it in items)
            {
                if (it is string s)
                {
                    listDest.Items.Remove(s);
                    _destSet.Remove(s);
                }
            }
        }

        

        private void RunCopyOrPreview()
        {
            logBox.Clear();

            var sources = GetSelectedSources();
            if (sources.Count == 0) { Log("ERROR: No source files selected."); return; }
            if (listDest.Items.Count == 0) { Log("ERROR: No destinations selected."); return; }

                        int prefixMode = cmbPrefixMode.SelectedIndex; // 0=None,1=Fixed,2=Numbered
            int suffixMode = cmbSuffixMode.SelectedIndex; // 0=None,1=Fixed,2=Numbered

            bool useFixed = (prefixMode == 1) && !string.IsNullOrWhiteSpace(txtPrefix.Text);
            bool useRange = (prefixMode == 2) && !string.IsNullOrWhiteSpace(txtPrefixBase.Text);

            bool useSuffixFixed = (suffixMode == 1) && !string.IsNullOrWhiteSpace(txtSuffix.Text);
            bool useSuffixRange = (suffixMode == 2) && !string.IsNullOrWhiteSpace(txtSuffixBase.Text);

            if (prefixMode == 1 && string.IsNullOrWhiteSpace(txtPrefix.Text)) { Log("ERROR: Fixed prefix enabled but empty."); return; }
            if (prefixMode == 2 && string.IsNullOrWhiteSpace(txtPrefixBase.Text)) { Log("ERROR: Numbered prefix enabled but base is empty."); return; }
            if (suffixMode == 1 && string.IsNullOrWhiteSpace(txtSuffix.Text)) { Log("ERROR: Suffix enabled but empty."); return; }
            if (suffixMode == 2 && string.IsNullOrWhiteSpace(txtSuffixBase.Text)) { Log("ERROR: Numbered suffix enabled but base is empty."); return; }

            int idxPrefix = (int)nudPrefixStart.Value;
            int idxSuffix = (int)nudSuffixStart.Value;

            var planned = new List<(string folder, string destFile, bool exists)>();
            foreach (var obj in listDest.Items.Cast<object?>())
            {
                var folder = obj as string;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    planned.Add((folder ?? "<null>", "<invalid>", false));
                    continue;
                }

                // Build suffix text per destination
                string suffixTextBase = string.Empty;
                if (useSuffixFixed) suffixTextBase = txtSuffix.Text ?? string.Empty;
                else if (useSuffixRange)
                {
                    var sNum = NamingHelpers.FormatRangeNumber(idxSuffix, (int)nudSuffixPad.Value);
                    suffixTextBase = (txtSuffixBase.Text ?? string.Empty) + sNum;
                }
                string prefixSep = (cmbPrefixSep.Visible && cmbPrefixSep.SelectedIndex > 0) ? (string)cmbPrefixSep.SelectedItem! : string.Empty;
                string suffixSep = (cmbSuffixSep.Visible && cmbSuffixSep.SelectedIndex > 0) ? (string)cmbSuffixSep.SelectedItem! : string.Empty;

                foreach (var srcFile in sources)
                {
                    string baseName = Path.GetFileName(srcFile);
                    string finalName = NamingHelpers.BuildTargetName(
                        baseName,
                        useFixed, txtPrefix.Text,
                        useRange, txtPrefixBase.Text, idxPrefix,
                        (useSuffixFixed || useSuffixRange), suffixTextBase,
                        prefixPadWidth: (int)nudPrefixPad.Value,
                        suffixPadWidth: (int)nudSuffixPad.Value,
                        prefixSeparator: prefixSep,
                        suffixSeparator: suffixSep);
                    var dest = Path.Combine(folder, finalName);

                    if (!IsValidFileName(finalName) || IsPathTooLong(dest))
                        planned.Add((folder, "<invalid>", false));
                    else
                        planned.Add((folder, dest, File.Exists(dest)));
                }
                if (useRange) idxPrefix++; if (useSuffixRange) idxSuffix++;
            }

            if (chkPreview.Checked)
            {
                int willOverwrite = planned.Count(p => p.exists);
                int invalid = planned.Count(p => p.destFile == "<invalid>");
                Log($"[PREVIEW] Sources: {sources.Count}");
                foreach (var p in planned)
                {
                    if (p.destFile == "<invalid>") Log($"[PREVIEW] Skipped -> {p.folder} (invalid name or path)");
                    else Log($"[PREVIEW] {p.destFile}" + (p.exists ? "  [will overwrite]" : ""));
                }
                Log($"[PREVIEW] Total targets: {planned.Count}, Overwrites: {willOverwrite}, Invalid: {invalid}");
                Status($"Preview only â€¢ {planned.Count} targets â€¢ {willOverwrite} overwrites");
                return;
            }

            var entry = new HistoryEntry
            {
                Sources = sources,
                Description = $"Copy {sources.Count} file(s) to {listDest.Items.Count} folder(s) at {DateTime.Now:t}",
                Overwrite = chkOverwrite.Checked
            };

            int copied = 0, failed = 0, skipped = 0, backedUp = 0;
            idxPrefix = (int)nudPrefixStart.Value;
            idxSuffix = (int)nudSuffixStart.Value;

            foreach (var obj in listDest.Items.Cast<object?>())
            {
                var folder = obj as string;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                { Log($"Skipped -> {folder} (folder missing)"); skipped++; continue; }

                // Compose once per destination
                string suffixTextBase = string.Empty;
                if (useSuffixFixed) suffixTextBase = txtSuffix.Text ?? string.Empty;
                else if (useSuffixRange)
                {
                    var sNum = NamingHelpers.FormatRangeNumber(idxSuffix, (int)nudSuffixPad.Value);
                    suffixTextBase = (txtSuffixBase.Text ?? string.Empty) + sNum;
                }
                string prefixSep = (cmbPrefixSep.Visible && cmbPrefixSep.SelectedIndex > 0) ? (string)cmbPrefixSep.SelectedItem! : string.Empty;
                string suffixSep = (cmbSuffixSep.Visible && cmbSuffixSep.SelectedIndex > 0) ? (string)cmbSuffixSep.SelectedItem! : string.Empty;

                foreach (var srcFile in sources)
                {
                    string baseName2 = Path.GetFileName(srcFile);
                    string finalName2 = NamingHelpers.BuildTargetName(
                        baseName2,
                        useFixed, txtPrefix.Text,
                        useRange, txtPrefixBase.Text, idxPrefix,
                        (useSuffixFixed || useSuffixRange), suffixTextBase,
                        prefixPadWidth: (int)nudPrefixPad.Value,
                        suffixPadWidth: (int)nudSuffixPad.Value,
                        prefixSeparator: prefixSep,
                        suffixSeparator: suffixSep);
                    var destPath = Path.Combine(folder, finalName2);

                    var rec = new CopyRecord { Destination = destPath, Source = srcFile };
                    try
                    {
                        if (File.Exists(destPath))
                        {
                            rec.HadExisting = true;
                            if (chkOverwrite.Checked)
                            {
                                if (IsIdentical(srcFile, destPath)) { Log($"Skipped (identical) -> {folder}"); skipped++; continue; }
                                rec.BackupPath = destPath + $".undo-bak-{DateTime.Now:yyyyMMddHHmmssfff}";
                                try { File.Copy(destPath, rec.BackupPath, overwrite: false); backedUp++; }
                                catch (Exception exBak) { Log($"WARN: Backup failed for {destPath}: {exBak.Message}"); }
                            }
                            else
                            {
                                Log($"Skipped -> {folder} (exists, overwrite off)");
                                skipped++; continue;
                            }
                        }

                        if (!CopyWithRetry(srcFile, destPath, chkOverwrite.Checked))
                            throw new IOException("Copy failed after retries");
                        entry.Ops.Add(rec);
                        Log($"Copied -> {folder}: {Path.GetFileName(destPath)}");
                        copied++;
                    }
                    catch (Exception ex)
                    {
                        Log($"FAILED -> {folder}: {ex.Message}");
                        if (!string.IsNullOrWhiteSpace(rec.BackupPath) && File.Exists(rec.BackupPath))
                        { try { File.Delete(rec.BackupPath); } catch { } rec.BackupPath = null; }
                        failed++;
                    }
                }

                if (useRange) idxPrefix++; if (useSuffixRange) idxSuffix++;
            }

            if (entry.Ops.Count > 0) { PushUndo(entry); _redo.Clear(); }

            Log($"Done. Copied: {copied}, Skipped: {skipped}, Failed: {failed}. Backups created: {backedUp}.");
            if (chkOverwrite.Checked && backedUp > 0)
                Log("Note: Undo will restore backups and remove new copies where appropriate.");
            if (!chkPreview.Checked)
            {
                if (chkAutoClearDest.Checked) { listDest.Items.Clear(); _destSet.Clear(); Log("Destinations auto-cleared."); }
                if (chkAutoClearSources.Checked) { _sources.Clear(); txtSource.Text = string.Empty; Log("Sources auto-cleared."); }
            }

            Status($"Copied {copied} â€¢ Skipped {skipped} â€¢ Failed {failed} â€¢ Undo: {_undo.Count} Redo: {_redo.Count}");
        }

        private void DoUndo()
        {
            if (_undo.Count == 0) { Status("Nothing to undo"); return; }

            var entry = _undo.Pop();
            int restored = 0, removed = 0, failed = 0;

            foreach (var rec in entry.Ops)
            {
                try
                {
                    if (rec.HadExisting && !string.IsNullOrWhiteSpace(rec.BackupPath) && File.Exists(rec.BackupPath))
                    {
                        if (File.Exists(rec.Destination)) { try { File.Delete(rec.Destination); } catch { } }
                        File.Move(rec.BackupPath, rec.Destination, overwrite: false);
                        restored++;
                    }
                    else
                    {
                        if (File.Exists(rec.Destination)) { File.Delete(rec.Destination); removed++; }
                    }
                }
                catch (Exception ex) { Log($"UNDO FAILED -> {rec.Destination}: {ex.Message}"); failed++; }
                finally
                {
                    if (!string.IsNullOrWhiteSpace(rec.BackupPath) && File.Exists(rec.BackupPath))
                    { try { File.Delete(rec.BackupPath); } catch { } }
                }
            }

            _redo.Push(entry);
            UpdateUndoRedoButtons();
            Log($"Undo: Restored {restored}, Removed {removed}, Failed {failed}.");
            Status($"Undid 1 step â€¢ Undo: {_undo.Count} Redo: {_redo.Count}");
        }

        private void DoRedo()
        {
            if (_redo.Count == 0) { Status("Nothing to redo"); return; }

            var entry = _redo.Pop();
            int copied = 0, skipped = 0, failed = 0, backedUp = 0;

            foreach (var rec in entry.Ops)
            {
                try
                {
                    if (File.Exists(rec.Destination))
                    {
                        if (!entry.Overwrite) { skipped++; continue; }
                        rec.BackupPath = rec.Destination + $".undo-bak-{DateTime.Now:yyyyMMddHHmmssfff}";
                        try { File.Copy(rec.Destination, rec.BackupPath, overwrite: false); backedUp++; }
                        catch (Exception exBak) { Log($"WARN: Backup failed for {rec.Destination}: {exBak.Message}"); }
                    }

                    File.Copy(rec.Source, rec.Destination, overwrite: entry.Overwrite);
                    copied++;
                }
                catch (Exception ex)
                {
                    Log($"REDO FAILED -> {rec.Destination}: {ex.Message}"); failed++;
                    if (!string.IsNullOrWhiteSpace(rec.BackupPath) && File.Exists(rec.BackupPath))
                    { try { File.Delete(rec.BackupPath); } catch { } rec.BackupPath = null; }
                }
            }

            PushUndo(entry);
            Log($"Redo: Copied {copied}, Skipped {skipped}, Failed {failed}. Backups created: {backedUp}.");
            Status($"Redid 1 step â€¢ Undo: {_undo.Count} Redo: {_redo.Count}");
        }

        private void PushUndo(HistoryEntry entry)
        {
            _undo.Push(entry);

            if (_undo.Count > HistoryCap)
            {
                var buffer = _undo.ToArray(); // newest-first ordering
                _undo.Clear();
                var limit = Math.Min(buffer.Length, HistoryCap);
                for (int i = limit - 1; i >= 0; i--)
                {
                    _undo.Push(buffer[i]);
                }
            }

            UpdateUndoRedoButtons();
        }

        private void UpdateUndoRedoButtons()
        {
            btnUndo.Enabled = _undo.Count > 0;
            btnRedo.Enabled = _redo.Count > 0;
        }

        private void Log(string msg) => logBox.AppendText((msg ?? string.Empty) + Environment.NewLine);
                private void LayoutBottom()
        {
            if (logBox == null) return;
            int marginLeft = 10, marginRight = 10, marginBottom = 12;
            int desiredHeight = 140; // larger default log area

            // Determine the bottom of the bottom-row controls (buttons/preview/mode dropdowns)
            int controlsBottom = 0;
            try
            {
                controlsBottom = new int[]
                {
                    btnUndo?.Bottom ?? 0,
                    btnRedo?.Bottom ?? 0,
                    btnEngage?.Bottom ?? 0,
                    chkPreview?.Bottom ?? 0,
                    cmbPrefixMode?.Bottom ?? 0,
                    cmbSuffixMode?.Bottom ?? 0
                }.Max();
            }
            catch { controlsBottom = 700; }

            int clientW = this.ClientSize.Width;
            int clientH = this.ClientSize.Height;

            // Log box should start below bottom controls with a small gap
            int minTop = controlsBottom + 10;
            int width = Math.Max(100, clientW - marginLeft - marginRight);
            int top = Math.Max(minTop, clientH - marginBottom - desiredHeight);
            int height = Math.Max(50, clientH - marginBottom - top);

            logBox.Left = marginLeft;
            logBox.Width = width;
            logBox.Top = top;
            logBox.Height = height;

            // Place status label just above the log box, aligned left
            if (lblStatus != null)
            {
                lblStatus.Left = marginLeft;
                int labelTop = logBox.Top - lblStatus.Height - 6;
                lblStatus.Top = Math.Max(controlsBottom + 2, labelTop);
            }

            // Right-align action buttons (Engage, Redo, Undo) with consistent gaps
            try
            {
                int gap = 10;
                int x = clientW - marginRight;
                if (btnEngage != null)
                {
                    btnEngage.Left = x - btnEngage.Width;
                    x = btnEngage.Left - gap;
                }
                if (btnRedo != null)
                {
                    btnRedo.Left = x - btnRedo.Width;
                    x = btnRedo.Left - gap;
                }
                if (btnUndo != null)
                {
                    btnUndo.Left = x - btnUndo.Width;
                }
            }
            catch { }
        }        private void Status(string msg) { lblStatus.Text = msg; }

        private string SettingsPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ManyCopy", "settings.txt");

        

        private void SaveLogToFile()
        {
            try
            {
                using var sfd = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", FileName = $"ManyCopy-Log-{DateTime.Now:yyyyMMdd-HHmmss}.txt" };
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, logBox.Text ?? string.Empty);
                    Status("Log saved");
                }
            }
            catch (Exception ex) { Status($"Save failed: {ex.Message}"); }
        }

        private void SaveTheme(ThemeMode mode)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(SettingsPath, $"{mode};{Theme.CurrentAccent}");
            }
            catch { }
        }

        private ThemeMode LoadTheme()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var s = (File.ReadAllText(SettingsPath) ?? "").Trim();
                    var parts = s.Split(';');
                    ThemeMode mode = ThemeMode.Auto;
                    if (parts.Length >= 1 && Enum.TryParse<ThemeMode>(parts[0], out var m)) mode = m;
                    if (parts.Length >= 2 && Enum.TryParse<AccentPreset>(parts[1], out var acc)) Theme.SetAccent(acc);
                    return mode;
                }
            }
            catch { }
            return ThemeMode.Auto;
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            try { PerformAutoScale(); LayoutBottom(); Invalidate(true); } catch { }
        }

        

        private static bool IsValidFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var invalid = Path.GetInvalidFileNameChars();
            return name.All(ch => !invalid.Contains(ch));
        }

        private static bool IsPathTooLong(string path)
        {
            try { return path.Length > 260; } catch { return false; }
        }

        private static bool IsIdentical(string src, string dest)
        {
            try
            {
                var si = new FileInfo(src);
                var di = new FileInfo(dest);
                return si.Length == di.Length && si.LastWriteTimeUtc == di.LastWriteTimeUtc;
            }
            catch { return false; }
        }

        private static bool CopyWithRetry(string src, string dest, bool overwrite, int retries = 3, int delayMs = 120)
        {
            for (int attempt = 0; attempt < retries; attempt++)
            {
                try
                {
                    File.Copy(src, dest, overwrite: overwrite);
                    return true;
                }
                catch
                {
                    if (attempt == retries - 1) return false;
                    System.Threading.Thread.Sleep(delayMs);
                }
            }
            return false;
        }

    }

    // ---------- Models ----------
    internal sealed class CopyRecord
    {
        public string Destination { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool HadExisting { get; set; } = false;
        public string? BackupPath { get; set; }
    }

    internal sealed class HistoryEntry
    {
        public List<string> Sources { get; set; } = new();
        public bool Overwrite { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<CopyRecord> Ops { get; set; } = new();
    }

    

    // ================= Explorer multi-folder picker (COM coclass) =================
    internal static class ShellFolderPicker
    {
        public static string[]? PickMultiple(IWin32Window owner)
        {
            var dlg = (IFileOpenDialog)new FileOpenDialog();

            int hr = dlg.GetOptions(out var opts);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);

            opts |= FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_ALLOWMULTISELECT;

            hr = dlg.SetOptions(opts);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);

            hr = dlg.SetTitle("Select one or more destination folders");
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);

            hr = dlg.Show(owner.Handle);
            if (hr == unchecked((int)0x800704C7)) return Array.Empty<string>(); // canceled
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);

            hr = dlg.GetResults(out var results);
            if (hr != 0 || results is null) return Array.Empty<string>();

            results.GetCount(out uint count);
            var paths = new string[count];

            for (uint i = 0; i < count; i++)
            {
                results.GetItemAt(i, out var item);
                try
                {
                    item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var psz);
                    var path = psz != IntPtr.Zero ? Marshal.PtrToStringUni(psz) ?? string.Empty : string.Empty;
                    if (psz != IntPtr.Zero) Marshal.FreeCoTaskMem(psz);
                    paths[i] = path;
                }
                finally
                {
                    if (item is not null) Marshal.ReleaseComObject(item);
                }
            }

            if (results is not null) Marshal.ReleaseComObject(results);
            Marshal.ReleaseComObject(dlg);
            return paths;
        }

        [ComImport, Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        private interface IFileDialog
        {
            [PreserveSig] int Show(IntPtr parent);
            [PreserveSig] int SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            [PreserveSig] int SetFileTypeIndex(uint iFileType);
            [PreserveSig] int GetFileTypeIndex(out uint piFileType);
            [PreserveSig] int Advise(IntPtr pfde, out uint pdwCookie);
            [PreserveSig] int Unadvise(uint pdwCookie);
            [PreserveSig] int SetOptions(FOS fos);
            [PreserveSig] int GetOptions(out FOS pfos);
            [PreserveSig] int SetDefaultFolder(IShellItem psi);
            [PreserveSig] int SetFolder(IShellItem psi);
            [PreserveSig] int GetFolder(out IShellItem ppsi);
            [PreserveSig] int GetCurrentSelection(out IShellItem ppsi);
            [PreserveSig] int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            [PreserveSig] int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            [PreserveSig] int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            [PreserveSig] int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            [PreserveSig] int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            [PreserveSig] int GetResult(out IShellItem ppsi);
            [PreserveSig] int AddPlace(IShellItem psi, FDAP fdap);
            [PreserveSig] int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            [PreserveSig] int Close(int hr);
            [PreserveSig] int SetClientGuid(ref Guid guid);
            [PreserveSig] int ClearClientData();
            [PreserveSig] int SetFilter(IntPtr pFilter);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
        private interface IFileOpenDialog : IFileDialog
        {
            [PreserveSig] new int Show(IntPtr parent);
            [PreserveSig] new int SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            [PreserveSig] new int SetFileTypeIndex(uint iFileType);
            [PreserveSig] new int GetFileTypeIndex(out uint piFileType);
            [PreserveSig] new int Advise(IntPtr pfde, out uint pdwCookie);
            [PreserveSig] new int Unadvise(uint pdwCookie);
            [PreserveSig] new int SetOptions(FOS fos);
            [PreserveSig] new int GetOptions(out FOS pfos);
            [PreserveSig] new int SetDefaultFolder(IShellItem psi);
            [PreserveSig] new int SetFolder(IShellItem psi);
            [PreserveSig] new int GetFolder(out IShellItem ppsi);
            [PreserveSig] new int GetCurrentSelection(out IShellItem ppsi);
            [PreserveSig] new int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            [PreserveSig] new int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            [PreserveSig] new int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            [PreserveSig] new int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            [PreserveSig] new int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            [PreserveSig] new int GetResult(out IShellItem ppsi);
            [PreserveSig] new int AddPlace(IShellItem psi, FDAP fdap);
            [PreserveSig] new int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            [PreserveSig] new int Close(int hr);
            [PreserveSig] new int SetClientGuid(ref Guid guid);
            [PreserveSig] new int ClearClientData();
            [PreserveSig] new int SetFilter(IntPtr pFilter);

            [PreserveSig] int GetResults(out IShellItemArray ppenum);
            [PreserveSig] int GetSelectedItems(out IShellItemArray ppsai);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        private interface IShellItem
        {
            [PreserveSig] int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int GetParent(out IShellItem ppsi);
            [PreserveSig] int GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
            [PreserveSig] int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            [PreserveSig] int Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
        private interface IShellItemArray
        {
            [PreserveSig] int BindToHandler(IntPtr pbc, ref Guid rbhid, ref Guid riid, out IntPtr ppvOut);
            [PreserveSig] int GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int GetPropertyDescriptionList(ref PROPERTYKEY keyType, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int GetAttributes(SIATTRIBFLAGS dwAttribFlags, uint sfgaoMask, out uint psfgaoAttribs);
            [PreserveSig] int GetCount(out uint pdwNumItems);
            [PreserveSig] int GetItemAt(uint dwIndex, out IShellItem ppsi);
            [PreserveSig] int EnumItems(out IntPtr ppenumShellItems);
        }

        private enum FDAP { FDAP_BOTTOM = 0, FDAP_TOP = 1 }
        [Flags] private enum FOS : uint { FOS_PICKFOLDERS = 0x20, FOS_FORCEFILESYSTEM = 0x40, FOS_ALLOWMULTISELECT = 0x200 }
        private enum SIGDN : uint { SIGDN_FILESYSPATH = 0x80058000 }
        [StructLayout(LayoutKind.Sequential, Pack = 4)] private struct PROPERTYKEY { public Guid fmtid; public uint pid; }
        [Flags] private enum SIATTRIBFLAGS { SIATTRIBFLAGS_AND = 1, SIATTRIBFLAGS_OR = 2, SIATTRIBFLAGS_APPCOMPAT = 3 }
    }
}













