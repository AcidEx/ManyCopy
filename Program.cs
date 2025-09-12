using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ManyCopy
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // --- DPI + modern visuals ---
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); // Per-monitor DPI
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    // =================== Theme Engine ===================
    public enum ThemeMode { Light, Dark, Auto }

    public static class Theme
    {
        public static ThemeMode Current { get; private set; } = ThemeMode.Auto;

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
                var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int v)
                    return v == 0 ? ThemeMode.Dark : ThemeMode.Light;
            }
            catch { }
            return ThemeMode.Light;
        }

        public static Palette Resolve(ThemeMode mode)
        {
            if (mode == ThemeMode.Auto) mode = DetectSystemTheme();
            if (mode == ThemeMode.Dark)
            {
                return new Palette
                {
                    Bg = Color.FromArgb(32, 32, 32),
                    Panel = Color.FromArgb(40, 40, 40),
                    Text = Color.Gainsboro,
                    Accent = Color.FromArgb(0, 122, 204),
                    InputBg = Color.FromArgb(28, 28, 28),
                    InputBorder = Color.FromArgb(70, 70, 70),
                    LogBg = Color.FromArgb(20, 20, 20),

                    ButtonBg = Color.FromArgb(60, 60, 60),
                    ButtonText = Color.Gainsboro,
                    ButtonBorder = Color.FromArgb(90, 90, 90),
                    ButtonBgHover = Color.FromArgb(72, 72, 72),
                    ButtonBgDown = Color.FromArgb(52, 52, 52),
                    DisabledText = Color.FromArgb(110, 110, 110),

                    PrimaryBg = Color.FromArgb(0, 122, 204),
                    PrimaryText = Color.White,
                    PrimaryHover = Color.FromArgb(10, 145, 235),
                    PrimaryDown = Color.FromArgb(0, 95, 175),
                    PrimaryBorder = Color.FromArgb(0, 95, 175),
                };
            }
            return new Palette();
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
                case ComboBox:
                    c.ForeColor = p.Text; c.BackColor = p.Panel; break;
            }

            if (c is Form f) f.BackColor = p.Bg;

            foreach (Control child in c.Controls) ApplyRecursive(child, p);
        }
    }

    // =================== Main App UI ===================
    public class MainForm : Form
    {
        private ComboBox cmbTheme = null!;
        private Button btnRefreshTheme = null!;

        private TextBox txtSource = null!;
        private Button btnBrowseSource = null!;

        private ListBox listDest = null!;
        private Button btnBrowseDest = null!;
        private Button btnRemoveSel = null!;
        private Button btnClear = null!;

        private CheckBox chkOverwrite = null!;
        private CheckBox chkUsePrefix = null!;
        private TextBox txtPrefix = null!;
        private CheckBox chkUsePrefixRange = null!;
        private TextBox txtPrefixBase = null!;
        private NumericUpDown nudPrefixStart = null!;
        private CheckBox chkUseSuffix = null!;
        private TextBox txtSuffix = null!;
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

        private readonly Stack<HistoryEntry> _undo = new();
        private readonly Stack<HistoryEntry> _redo = new();
        private const int HistoryCap = 100;

        public MainForm()
        {
            Text = "ManyCopy v1.1.2 — Copy Files to Many Folders";
            // ---- DPI-friendly form baseline ----
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);   // 100% DPI baseline
            Font = new Font("Segoe UI", 9f);             // Good scaling font
            MinimumSize = new Size(980, 760);            // Prevent cramped layouts

            Width = 1000;
            Height = 880;
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;

            // Theme controls
            cmbTheme = new ComboBox { Left = 820, Top = 12, Width = 155, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTheme.Items.AddRange(new object[] { "Auto (Windows)", "Light", "Dark" });
            cmbTheme.SelectedIndexChanged += (_, __) =>
            {
                var mode = cmbTheme.SelectedIndex switch
                {
                    1 => ThemeMode.Light,
                    2 => ThemeMode.Dark,
                    _ => ThemeMode.Auto
                };
                Theme.ApplyTo(this, mode);
                SaveTheme(mode);
            };
            cmbTheme.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(cmbTheme);

            btnRefreshTheme = new Button { Text = "Refresh Theme", Left = 700, Top = 11, Width = 110, Height = 24 };
            btnRefreshTheme.Click += (_, __) =>
            {
                var mode = cmbTheme.SelectedIndex switch
                {
                    1 => ThemeMode.Light,
                    2 => ThemeMode.Dark,
                    _ => ThemeMode.Auto
                };
                Theme.ApplyTo(this, mode);
            };
            btnRefreshTheme.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(btnRefreshTheme);

            // Source
            var lblSource = new Label { Text = "Source file:", Left = 10, Top = 50, AutoSize = true };
            txtSource = new TextBox { Left = 95, Top = 47, Width = 780, AllowDrop = true };
            txtSource.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            btnBrowseSource = new Button { Text = "Browse…", Left = 885, Top = 46, Width = 90 };
            btnBrowseSource.Click += (_, __) => PickSource();
            btnBrowseSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            txtSource.DragEnter += (s, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true &&
                    e.Data.GetData(DataFormats.FileDrop) is string[] paths &&
                    paths.Length == 1 && File.Exists(paths[0]))
                    e.Effect = DragDropEffects.Copy;
            };
            txtSource.DragDrop += (s, e) =>
            {
                if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths &&
                    paths.Length == 1 && File.Exists(paths[0]))
                    txtSource.Text = paths[0];
            };

            Controls.AddRange(new Control[] { lblSource, txtSource, btnBrowseSource });

            // Range Helper
            chkEnableRange = new CheckBox { Text = "Enable Range Helper", Left = 10, Top = 80, AutoSize = true };
            chkEnableRange.CheckedChanged += (_, __) => grpRange.Visible = chkEnableRange.Checked;
            Controls.Add(chkEnableRange);

            grpRange = new GroupBox { Text = "Range Helper", Left = 10, Top = 105, Width = 965, Height = 120, Visible = false };
            grpRange.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblRoot = new Label { Text = "Root:", Left = 10, Top = 25, AutoSize = true };
            txtRoot = new TextBox { Left = 60, Top = 22, Width = 800, Text = "" };
            txtRoot.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            btnBrowseRoot = new Button { Text = "Browse…", Left = 870, Top = 21, Width = 85 };
            btnBrowseRoot.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseRoot.Click += (_, __) => PickRoot();

            var lblRangePrefix = new Label { Text = "Prefix:", Left = 10, Top = 60, AutoSize = true };
            txtRangePrefix = new TextBox { Left = 60, Top = 57, Width = 150, Text = "" };

            var lblStart = new Label { Text = "Start #:", Left = 230, Top = 60, AutoSize = true };
            txtStart = new TextBox { Left = 285, Top = 57, Width = 80, Text = "" };

            var lblEnd = new Label { Text = "End #:", Left = 380, Top = 60, AutoSize = true };
            txtEnd = new TextBox { Left = 430, Top = 57, Width = 80, Text = "" };

            chkCreateMissing = new CheckBox { Text = "Create missing folders", Left = 530, Top = 59, AutoSize = true };

            btnAddRange = new Button { Text = "Add Range →", Left = 760, Top = 56, Width = 195 };
            btnAddRange.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddRange.Click += (_, __) => AddRangeToList();

            grpRange.Controls.AddRange(new Control[]
            {
                lblRoot, txtRoot, btnBrowseRoot,
                lblRangePrefix, txtRangePrefix, lblStart, txtStart, lblEnd, txtEnd,
                chkCreateMissing, btnAddRange
            });
            Controls.Add(grpRange);

            // Destinations
            var lblDest = new Label { Text = "Destination folders:", Left = 10, Top = 235, AutoSize = true };
            Controls.Add(lblDest);

            listDest = new ListBox
            {
                Left = 10,
                Top = 255,
                Width = 860,
                Height = 400,
                SelectionMode = SelectionMode.MultiExtended,
                AllowDrop = true
            };
            listDest.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

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

            btnBrowseDest = new Button { Text = "Browse…", Left = 885, Top = 255, Width = 90 };
            btnBrowseDest.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseDest.Click += (_, __) => AddMultipleFolders();

            btnRemoveSel = new Button { Text = "Remove", Left = 885, Top = 290, Width = 90 };
            btnRemoveSel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRemoveSel.Click += (_, __) => RemoveSelected();

            btnClear = new Button { Text = "Clear All", Left = 885, Top = 325, Width = 90 };
            btnClear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClear.Click += (_, __) => listDest.Items.Clear();

            Controls.AddRange(new Control[] { listDest, btnBrowseDest, btnRemoveSel, btnClear });

            // Options
            chkOverwrite = new CheckBox { Text = "Overwrite if exists", Left = 10, Top = 670, AutoSize = true };
            Controls.Add(chkOverwrite);

            chkUsePrefix = new CheckBox { Text = "Fixed prefix", Left = 160, Top = 670, AutoSize = true };
            Controls.Add(chkUsePrefix);

            txtPrefix = new TextBox { Left = 255, Top = 667, Width = 140, Enabled = false, Text = "" };
            txtPrefix.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(txtPrefix);

            chkUsePrefix.CheckedChanged += (_, __) =>
            {
                txtPrefix.Enabled = chkUsePrefix.Checked;
                if (chkUsePrefix.Checked)
                {
                    chkUsePrefixRange.Checked = false;
                    txtPrefixBase.Enabled = false; nudPrefixStart.Enabled = false;
                }
            };

            chkUsePrefixRange = new CheckBox { Text = "Numbered prefix", Left = 405, Top = 670, AutoSize = true };
            Controls.Add(chkUsePrefixRange);

            txtPrefixBase = new TextBox { Left = 530, Top = 667, Width = 100, Enabled = false, Text = "" };
            txtPrefixBase.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(txtPrefixBase);

            var lblStartNum = new Label { Text = "Start:", Left = 635, Top = 670, AutoSize = true };
            Controls.Add(lblStartNum);

            nudPrefixStart = new NumericUpDown { Left = 675, Top = 667, Width = 70, Minimum = 0, Maximum = 1_000_000, Value = 1, Enabled = false };
            nudPrefixStart.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(nudPrefixStart);

            chkUsePrefixRange.CheckedChanged += (_, __) =>
            {
                var on = chkUsePrefixRange.Checked;
                txtPrefixBase.Enabled = on; nudPrefixStart.Enabled = on;
                if (on) { chkUsePrefix.Checked = false; txtPrefix.Enabled = false; }
            };

            chkUseSuffix = new CheckBox { Text = "Suffix", Left = 760, Top = 670, AutoSize = true };
            Controls.Add(chkUseSuffix);

            txtSuffix = new TextBox { Left = 820, Top = 667, Width = 160, Enabled = false, Text = "" };
            txtSuffix.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(txtSuffix);

            chkUseSuffix.CheckedChanged += (_, __) => txtSuffix.Enabled = chkUseSuffix.Checked;

            chkPreview = new CheckBox { Text = "Preview mode", Left = 10, Top = 700, AutoSize = true };
            Controls.Add(chkPreview);

            // Actions + log
            btnUndo = new Button { Text = "Undo", Left = 610, Top = 696, Width = 90, Height = 32, Enabled = false };
            btnUndo.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnUndo.Click += (_, __) => DoUndo();

            btnRedo = new Button { Text = "Redo", Left = 710, Top = 696, Width = 90, Height = 32, Enabled = false };
            btnRedo.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnRedo.Click += (_, __) => DoRedo();

            btnEngage = new Button { Text = "Engage", Left = 810, Top = 694, Width = 120, Height = 36, Tag = "primary" };
            btnEngage.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnEngage.Click += (_, __) => RunCopyOrPreview();

            lblStatus = new Label { Left = 10, Top = 730, AutoSize = true, Text = "Ready" };
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            logBox = new TextBox
            {
                Left = 10,
                Top = 755,
                Width = 965,
                Height = 90,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };
            logBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.AddRange(new Control[] { btnUndo, btnRedo, btnEngage, lblStatus, logBox });

            // Load/apply theme
            var saved = LoadTheme();
            cmbTheme.SelectedIndex = saved switch { ThemeMode.Light => 1, ThemeMode.Dark => 2, _ => 0 };
            Theme.ApplyTo(this, saved);
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z) { DoUndo(); e.Handled = true; }
            else if (e.Control && (e.KeyCode == Keys.Y || (e.Shift && e.KeyCode == Keys.Z))) { DoRedo(); e.Handled = true; }
        }

        private void PickSource()
        {
            using var ofd = new OpenFileDialog { Title = "Select source file", Filter = "All files (*.*)|*.*" };
            if (ofd.ShowDialog(this) == DialogResult.OK) txtSource.Text = ofd.FileName;
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
            if (!int.TryParse((txtStart.Text ?? "").Trim(), out var start) ||
                !int.TryParse((txtEnd.Text ?? "").Trim(), out var end)) { Log("ERROR: Start/End must be whole numbers."); return; }
            if (start > end) { Log("ERROR: Start number greater than end number."); return; }

            int added = 0, created = 0;
            for (int i = start; i <= end; i++)
            {
                var folder = Path.Combine(root, $"{prefix}{i}");
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
            Log($"Range added: {added} folders. Created new: {created}");
        }

        private void AddMultipleFolders()
        {
            var paths = ShellFolderPicker.PickMultiple(this);
            if (paths == null || paths.Length == 0) return;
            foreach (var p in paths.Where(Directory.Exists)) AddDestPath(p);
        }

        private void AddDestPath(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path) && !listDest.Items.Contains(path))
                listDest.Items.Add(path);
        }

        private void RemoveSelected()
        {
            var items = listDest.SelectedItems.Cast<object?>().ToList();
            foreach (var it in items) if (it is not null) listDest.Items.Remove(it);
        }

        private void RunCopyOrPreview()
        {
            logBox.Clear();

            var src = (txtSource.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(src) || !File.Exists(src)) { Log("ERROR: Source file not found."); return; }
            if (listDest.Items.Count == 0) { Log("ERROR: No destinations selected."); return; }

            bool useFixed = chkUsePrefix.Checked && !string.IsNullOrWhiteSpace(txtPrefix.Text);
            bool useRange = chkUsePrefixRange.Checked && !string.IsNullOrWhiteSpace(txtPrefixBase.Text);
            bool useSuffix = chkUseSuffix.Checked && !string.IsNullOrWhiteSpace(txtSuffix.Text);

            if (chkUsePrefix.Checked && string.IsNullOrWhiteSpace(txtPrefix.Text)) { Log("ERROR: Fixed prefix enabled but empty."); return; }
            if (chkUsePrefixRange.Checked && string.IsNullOrWhiteSpace(txtPrefixBase.Text)) { Log("ERROR: Numbered prefix enabled but base is empty."); return; }
            if (chkUseSuffix.Checked && string.IsNullOrWhiteSpace(txtSuffix.Text)) { Log("ERROR: Suffix enabled but empty."); return; }

            int idx = (int)nudPrefixStart.Value;
            string baseName = Path.GetFileName(src);

            var planned = new List<(string folder, string destFile, bool exists)>();
            foreach (var obj in listDest.Items.Cast<object?>())
            {
                var folder = obj as string;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    planned.Add((folder ?? "<null>", "<invalid>", false));
                    continue;
                }

                string finalName = BuildTargetName(baseName, useFixed, txtPrefix.Text, useRange, txtPrefixBase.Text, idx, useSuffix, txtSuffix.Text);
                var dest = Path.Combine(folder, finalName);

                planned.Add((folder, dest, File.Exists(dest)));
                if (useRange) idx++;
            }

            if (chkPreview.Checked)
            {
                int willOverwrite = planned.Count(p => p.exists);
                int invalid = planned.Count(p => p.destFile == "<invalid>");
                Log($"[PREVIEW] Source: {src}");
                foreach (var p in planned)
                {
                    if (p.destFile == "<invalid>") Log($"[PREVIEW] Skipped -> {p.folder} (folder missing)");
                    else Log($"[PREVIEW] {p.destFile}" + (p.exists ? "  [will overwrite]" : ""));
                }
                Log($"[PREVIEW] Total targets: {planned.Count}, Overwrites: {willOverwrite}, Invalid: {invalid}");
                Status($"Preview only • {planned.Count} targets • {willOverwrite} overwrites");
                return;
            }

            var entry = new HistoryEntry
            {
                Source = src,
                Description = $"Copy to {planned.Count} folder(s) at {DateTime.Now:t}",
                Overwrite = chkOverwrite.Checked
            };

            int copied = 0, failed = 0, skipped = 0, backedUp = 0;
            foreach (var p in planned)
            {
                if (!Directory.Exists(p.folder))
                {
                    Log($"Skipped -> {p.folder} (folder missing)");
                    skipped++;
                    continue;
                }

                var rec = new CopyRecord { Destination = p.destFile };
                try
                {
                    if (File.Exists(p.destFile))
                    {
                        rec.HadExisting = true;
                        if (chkOverwrite.Checked)
                        {
                            rec.BackupPath = p.destFile + $".undo-bak-{DateTime.Now:yyyyMMddHHmmssfff}";
                            try { File.Copy(p.destFile, rec.BackupPath, overwrite: false); backedUp++; }
                            catch (Exception exBak) { Log($"WARN: Backup failed for {p.destFile}: {exBak.Message}"); }
                        }
                        else
                        {
                            Log($"Skipped -> {p.folder} (exists, overwrite off)");
                            skipped++;
                            continue;
                        }
                    }

                    File.Copy(src, p.destFile, overwrite: chkOverwrite.Checked);
                    entry.Ops.Add(rec);
                    Log($"Copied -> {p.folder}");
                    copied++;
                }
                catch (Exception ex)
                {
                    Log($"FAILED -> {p.folder}: {ex.Message}");
                    if (!string.IsNullOrWhiteSpace(rec.BackupPath) && File.Exists(rec.BackupPath))
                    { try { File.Delete(rec.BackupPath); } catch { } rec.BackupPath = null; }
                    failed++;
                }
            }

            if (entry.Ops.Count > 0)
            {
                PushUndo(entry);
                _redo.Clear();
            }

            Log($"Done. Copied: {copied}, Skipped: {skipped}, Failed: {failed}. Backups created: {backedUp}.");
            if (chkOverwrite.Checked && backedUp > 0)
                Log("Note: Undo will restore backups and remove new copies where appropriate.");

            Status($"Copied {copied} • Skipped {skipped} • Failed {failed} • Undo:{_undo.Count} Redo:{_redo.Count}");
        }

        private static string BuildTargetName(
            string baseName,
            bool useFixed, string? fixedPrefix,
            bool useRange, string? rangeBase, int index,
            bool useSuffix, string? suffix)
        {
            string prefixPart = "";
            if (useRange) prefixPart = (rangeBase ?? "").Trim() + index.ToString();
            else if (useFixed) prefixPart = (fixedPrefix ?? "").Trim();

            string nameNoExt = Path.GetFileNameWithoutExtension(baseName);
            string ext = Path.GetExtension(baseName);
            string suffixPart = useSuffix ? (suffix ?? "").Trim() : "";
            if (!string.IsNullOrEmpty(suffixPart)) nameNoExt = nameNoExt + suffixPart;

            return prefixPart + nameNoExt + ext;
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
            Status($"Undid 1 step • Undo:{_undo.Count} Redo:{_redo.Count}");
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

                    File.Copy(entry.Source, rec.Destination, overwrite: entry.Overwrite);
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
            Status($"Redid 1 step • Undo:{_undo.Count} Redo:{_redo.Count}");
        }

        private void PushUndo(HistoryEntry entry)
        {
            _undo.Push(entry);
            while (_undo.Count > HistoryCap) _undo.TrimExcess();
            UpdateUndoRedoButtons();
        }

        private void UpdateUndoRedoButtons()
        {
            btnUndo.Enabled = _undo.Count > 0;
            btnRedo.Enabled = _redo.Count > 0;
        }

        private void Log(string msg) => logBox.AppendText((msg ?? string.Empty) + Environment.NewLine);
        private void Status(string msg) { lblStatus.Text = msg; }

        private string SettingsPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ManyCopy", "settings.txt");

        private void SaveTheme(ThemeMode mode)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(SettingsPath, mode.ToString());
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
                    if (Enum.TryParse<ThemeMode>(s, out var m)) return m;
                }
            }
            catch { }
            return ThemeMode.Auto;
        }
    }

    // ---------- Models ----------
    internal sealed class CopyRecord
    {
        public string Destination { get; set; } = string.Empty;
        public bool HadExisting { get; set; } = false;
        public string? BackupPath { get; set; }
    }

    internal sealed class HistoryEntry
    {
        public string Source { get; set; } = string.Empty;
        public bool Overwrite { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<CopyRecord> Ops { get; set; } = new();
    }

    // ==================== Explorer multi-folder picker (coclass-based, corrected GUIDs) ====================
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

        // ✅ Correct GUID for IShellItemArray
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

    // ============= Small UI helpers =============
    internal static class UiExt
    {
        public static void ToolTip(this Control ctrl, string text)
        {
            var tip = new ToolTip { AutomaticDelay = 300, AutoPopDelay = 8000, InitialDelay = 300, ReshowDelay = 100 };
            tip.SetToolTip(ctrl, text);
        }
    }
}
