using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PSJpgExporterApp
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private Label statusLabel;
        private Label versionLabel;
        private TextBox documentBox;
        private TextBox sourceFolderBox;
        private TextBox outputFolderBox;
        private NumericUpDown qualityBox;
        private CheckBox useSourceFolderBox;
        private CheckBox overwriteBox;
        private Button refreshButton;
        private Button chooseFolderButton;
        private Button exportButton;
        private Button openFolderButton;
        private TextBox logBox;

        private string lastOutputFolder = "";

        public MainForm()
        {
            Text = "PS-JPG 图层加背景导出器";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(980, 720);
            MinimumSize = new Size(900, 660);
            BackColor = Theme.AppBackground;
            Font = new Font("Microsoft YaHei UI", 9F);
            Icon = LogoFactory.CreateIcon();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                RowCount = 5,
                ColumnCount = 1,
                BackColor = Theme.AppBackground
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 182));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildDocumentPanel(), 0, 1);
            root.Controls.Add(BuildSettingsPanel(), 0, 2);
            root.Controls.Add(BuildActionPanel(), 0, 3);
            root.Controls.Add(BuildLogPanel(), 0, 4);

            AppendLog("准备就绪。请先打开 Photoshop 和要处理的文件，然后点击“刷新状态”或“开始导出”。");
            RefreshPhotoshopStatus();
        }

        private Control BuildHeader()
        {
            var header = new GradientPanel
            {
                Dock = DockStyle.Fill,
                Radius = 8,
                StartColor = Color.FromArgb(18, 37, 63),
                EndColor = Color.FromArgb(25, 116, 96),
                Margin = new Padding(0, 0, 0, 12)
            };

            var logo = new PictureBox
            {
                Image = LogoFactory.CreateBitmap(72),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(72, 72),
                Location = new Point(24, 20)
            };
            header.Controls.Add(logo);

            var title = new Label
            {
                Text = "PS-JPG 图层加背景导出器",
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(112, 24)
            };
            header.Controls.Add(title);

            var subtitle = new Label
            {
                Text = "连接 Photoshop，按“背景图层 + 当前图层”的规则批量导出 JPG。",
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 9.5F),
                ForeColor = Color.FromArgb(222, 238, 232),
                BackColor = Color.Transparent,
                Location = new Point(114, 66)
            };
            header.Controls.Add(subtitle);

            var badge = new Label
            {
                Text = "Windows · Photoshop COM",
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(42, 142, 115),
                Padding = new Padding(10, 5, 10, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            badge.Location = new Point(730, 26);
            badge.Resize += delegate { badge.Left = header.Width - badge.Width - 24; };
            header.Resize += delegate { badge.Left = header.Width - badge.Width - 24; };
            header.Controls.Add(badge);

            return header;
        }

        private Control BuildDocumentPanel()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 16),
                ColumnCount = 4,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            card.Controls.Add(layout);

            var section = MakeSectionTitle("当前 Photoshop 文档");
            layout.Controls.Add(section, 0, 0);
            layout.SetColumnSpan(section, 2);

            refreshButton = MakeSecondaryButton("刷新状态");
            refreshButton.Dock = DockStyle.Fill;
            refreshButton.Margin = new Padding(0, 0, 0, 8);
            refreshButton.Click += delegate { RefreshPhotoshopStatus(); };
            layout.Controls.Add(refreshButton, 3, 0);

            layout.Controls.Add(MakeLabel("连接状态"), 0, 1);
            statusLabel = MakeStatusLabel("尚未检测");
            layout.Controls.Add(statusLabel, 1, 1);

            layout.Controls.Add(MakeLabel("PS 版本"), 2, 1);
            versionLabel = MakeStatusLabel("-");
            layout.Controls.Add(versionLabel, 3, 1);

            layout.Controls.Add(MakeLabel("文档"), 0, 2);
            documentBox = MakeTextBox();
            layout.Controls.Add(documentBox, 1, 2);
            layout.SetColumnSpan(documentBox, 3);

            layout.Controls.Add(MakeLabel("原位置"), 0, 3);
            sourceFolderBox = MakeTextBox();
            layout.Controls.Add(sourceFolderBox, 1, 3);
            layout.SetColumnSpan(sourceFolderBox, 3);

            return card;
        }

        private Control BuildSettingsPanel()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 16),
                ColumnCount = 5,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            card.Controls.Add(layout);

            var section = MakeSectionTitle("导出设置");
            layout.Controls.Add(section, 0, 0);
            layout.SetColumnSpan(section, 5);

            layout.Controls.Add(MakeLabel("JPG 质量"), 0, 1);
            qualityBox = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 12,
                Value = 12,
                Width = 80,
                Height = 28,
                Anchor = AnchorStyles.Left,
                BorderStyle = BorderStyle.FixedSingle
            };
            layout.Controls.Add(qualityBox, 1, 1);

            overwriteBox = new CheckBox
            {
                Text = "覆盖同名 JPG",
                Checked = true,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            layout.Controls.Add(overwriteBox, 2, 1);

            var qualityHint = new Label
            {
                Text = "质量 12 为最高导出质量；关闭覆盖后会自动追加 _2、_3。",
                AutoSize = true,
                ForeColor = Theme.MutedText,
                Anchor = AnchorStyles.Left
            };
            layout.Controls.Add(qualityHint, 3, 1);
            layout.SetColumnSpan(qualityHint, 2);

            useSourceFolderBox = new CheckBox
            {
                Text = "导出到原始图片所在文件夹",
                Checked = true,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            useSourceFolderBox.CheckedChanged += delegate
            {
                outputFolderBox.BackColor = useSourceFolderBox.Checked ? Color.FromArgb(248, 250, 250) : Color.White;
                chooseFolderButton.Enabled = !useSourceFolderBox.Checked;
                chooseFolderButton.Visible = !useSourceFolderBox.Checked;
                if (useSourceFolderBox.Checked)
                {
                    outputFolderBox.Text = "";
                }
                else if (string.IsNullOrWhiteSpace(outputFolderBox.Text) &&
                         !string.IsNullOrWhiteSpace(sourceFolderBox.Text) &&
                         !sourceFolderBox.Text.StartsWith("当前文档"))
                {
                    outputFolderBox.Text = sourceFolderBox.Text;
                }
            };
            layout.Controls.Add(useSourceFolderBox, 0, 2);
            layout.SetColumnSpan(useSourceFolderBox, 5);

            layout.Controls.Add(MakeLabel("自选位置"), 0, 3);
            outputFolderBox = MakeTextBox();
            outputFolderBox.BackColor = Color.FromArgb(248, 250, 250);
            layout.Controls.Add(outputFolderBox, 1, 3);
            layout.SetColumnSpan(outputFolderBox, 3);

            chooseFolderButton = MakeSecondaryButton("浏览...");
            chooseFolderButton.Enabled = false;
            chooseFolderButton.Visible = false;
            chooseFolderButton.Dock = DockStyle.Fill;
            chooseFolderButton.Margin = new Padding(8, 2, 0, 2);
            chooseFolderButton.Click += delegate { ChooseOutputFolder(); };
            layout.Controls.Add(chooseFolderButton, 4, 3);

            return card;
        }

        private Control BuildActionPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 8),
                BackColor = Theme.AppBackground
            };

            exportButton = MakePrimaryButton("开始导出");
            exportButton.Click += delegate { StartExport(); };
            panel.Controls.Add(exportButton);

            openFolderButton = MakeSecondaryButton("打开输出文件夹");
            openFolderButton.Width = 150;
            openFolderButton.Enabled = false;
            openFolderButton.Click += delegate { OpenLastOutputFolder(); };
            panel.Controls.Add(openFolderButton);

            var githubButton = MakeSecondaryButton("打开 GitHub");
            githubButton.Width = 122;
            githubButton.Click += delegate { Process.Start("https://github.com/pzk1221/PS-JPG"); };
            panel.Controls.Add(githubButton);

            return panel;
        }

        private Control BuildLogPanel()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 18),
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(layout);

            layout.Controls.Add(MakeSectionTitle("运行日志"), 0, 0);
            logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 252, 252),
                ForeColor = Color.FromArgb(32, 40, 44),
                Font = new Font("Consolas", 10F)
            };
            layout.Controls.Add(logBox, 0, 1);

            return card;
        }

        private static Label MakeSectionTitle(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                ForeColor = Theme.TitleText,
                Anchor = AnchorStyles.Left
            };
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Theme.MutedText
            };
        }

        private static Label MakeStatusLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Theme.TitleText
            };
        }

        private static TextBox MakeTextBox()
        {
            return new TextBox
            {
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(3, 4, 3, 4)
            };
        }

        private static Button MakePrimaryButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Width = 142,
                Height = 36,
                Margin = new Padding(0, 4, 10, 4),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(32, 144, 104);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 104, 76);
            return button;
        }

        private static Button MakeSecondaryButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Width = 118,
                Height = 32,
                Margin = new Padding(0, 4, 10, 4),
                BackColor = Color.White,
                ForeColor = Theme.TitleText,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderColor = Theme.Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 249, 248);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(230, 240, 238);
            return button;
        }

        private void ChooseOutputFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择 JPG 导出位置";
                if (!string.IsNullOrWhiteSpace(outputFolderBox.Text) && Directory.Exists(outputFolderBox.Text))
                {
                    dialog.SelectedPath = outputFolderBox.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    outputFolderBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void RefreshPhotoshopStatus()
        {
            SetBusy(true, "正在检测 Photoshop...");
            RunSta(
                delegate
                {
                    PhotoshopConnection connection = GetPhotoshop();
                    string status = Convert.ToString(connection.Application.GetType().InvokeMember(
                        "DoJavaScript",
                        BindingFlags.InvokeMethod,
                        null,
                        connection.Application,
                        new object[] { BuildStatusScript() }));
                    return ParsePhotoshopInfo(status);
                },
                delegate(object result)
                {
                    var info = (PhotoshopInfo)result;
                    statusLabel.Text = info.Status;
                    versionLabel.Text = string.IsNullOrWhiteSpace(info.Version) ? "-" : info.Version;
                    documentBox.Text = info.DocumentName;
                    sourceFolderBox.Text = info.HasSourceFolder ? info.SourceFolder : "当前文档未保存，无法判断原位置";
                    if (!useSourceFolderBox.Checked && string.IsNullOrWhiteSpace(outputFolderBox.Text) && info.HasSourceFolder)
                    {
                        outputFolderBox.Text = info.SourceFolder;
                    }
                    AppendLog("检测完成：" + info.Status + (string.IsNullOrWhiteSpace(info.Version) ? "" : " Photoshop " + info.Version));
                    SetBusy(false, info.Status);
                },
                delegate(Exception ex)
                {
                    statusLabel.Text = "无法连接 Photoshop。";
                    versionLabel.Text = "-";
                    documentBox.Text = "";
                    sourceFolderBox.Text = "";
                    AppendLog("检测失败：" + ex.Message);
                    SetBusy(false, "无法连接 Photoshop。");
                });
        }

        private void StartExport()
        {
            int quality = (int)qualityBox.Value;
            bool useSourceFolder = useSourceFolderBox.Checked;
            bool overwrite = overwriteBox.Checked;
            string customFolder = outputFolderBox.Text.Trim();

            if (!useSourceFolder && string.IsNullOrWhiteSpace(customFolder))
            {
                MessageBox.Show(this, "请先选择自定义导出文件夹。", "缺少导出位置", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true, "正在导出，请稍候...");
            AppendLog("开始导出。");

            RunSta(
                delegate
                {
                    PhotoshopConnection connection = GetPhotoshop();
                    string script = BuildPhotoshopScript(quality, useSourceFolder, customFolder, overwrite);
                    object result = connection.Application.GetType().InvokeMember(
                        "DoJavaScript",
                        BindingFlags.InvokeMethod,
                        null,
                        connection.Application,
                        new object[] { script });
                    return Convert.ToString(result);
                },
                delegate(object result)
                {
                    string message = Convert.ToString(result);
                    AppendLog(message);
                    bool failed = message.StartsWith("失败", StringComparison.OrdinalIgnoreCase);
                    lastOutputFolder = ExtractFolder(message);
                    openFolderButton.Enabled = !failed && Directory.Exists(lastOutputFolder);
                    SetBusy(false, failed ? "导出失败。" : "导出成功。");
                    MessageBox.Show(
                        this,
                        message,
                        failed ? "失败" : "成功",
                        MessageBoxButtons.OK,
                        failed ? MessageBoxIcon.Error : MessageBoxIcon.Information);
                    RefreshPhotoshopStatus();
                },
                delegate(Exception ex)
                {
                    string reason = FormatFailureReason(ex);
                    AppendLog("导出失败：" + reason);
                    SetBusy(false, "导出失败。");
                    MessageBox.Show(this, "失败：导出没有完成\r\n\r\n原因：" + reason, "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
        }

        private void OpenLastOutputFolder()
        {
            if (Directory.Exists(lastOutputFolder))
            {
                Process.Start(lastOutputFolder);
            }
        }

        private void SetBusy(bool busy, string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool, string>(SetBusy), busy, status);
                return;
            }

            statusLabel.Text = status;
            refreshButton.Enabled = !busy;
            exportButton.Enabled = !busy;
            chooseFolderButton.Enabled = !busy && !useSourceFolderBox.Checked;
            useSourceFolderBox.Enabled = !busy;
            overwriteBox.Enabled = !busy;
            qualityBox.Enabled = !busy;
        }

        private void AppendLog(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), message);
                return;
            }

            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
        }

        private void RunSta(Func<object> work, Action<object> success, Action<Exception> fail)
        {
            var owner = this;
            var thread = new Thread(
                delegate()
                {
                    try
                    {
                        object result = work();
                        if (!owner.IsDisposed && owner.IsHandleCreated)
                        {
                            owner.BeginInvoke(success, result);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!owner.IsDisposed && owner.IsHandleCreated)
                        {
                            owner.BeginInvoke(fail, ex);
                        }
                    }
                });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        private static PhotoshopConnection GetPhotoshop()
        {
            List<string> progIds = DiscoverPhotoshopProgIds();
            Exception lastError = null;

            foreach (string progId in progIds)
            {
                try
                {
                    object active = Marshal.GetActiveObject(progId);
                    return new PhotoshopConnection(active, progId);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            foreach (string progId in progIds)
            {
                try
                {
                    Type type = Type.GetTypeFromProgID(progId);
                    if (type == null)
                    {
                        continue;
                    }
                    object created = Activator.CreateInstance(type);
                    return new PhotoshopConnection(created, progId);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            string suffix = lastError == null ? "" : " 最后错误：" + lastError.Message;
            throw new InvalidOperationException("没有找到可连接的 Photoshop。请确认已安装 Photoshop，并关闭 Photoshop 中的弹窗后重试。" + suffix);
        }

        private static List<string> DiscoverPhotoshopProgIds()
        {
            var list = new List<string>();
            AddUnique(list, "Photoshop.Application");

            try
            {
                using (RegistryKey classes = Registry.ClassesRoot)
                {
                    foreach (string name in classes.GetSubKeyNames())
                    {
                        if (name.Equals("Photoshop.Application", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("Photoshop.Application.", StringComparison.OrdinalIgnoreCase))
                        {
                            AddUnique(list, name);
                        }
                    }
                }
            }
            catch
            {
            }

            list.Sort(
                delegate(string a, string b)
                {
                    if (a == "Photoshop.Application") return -1;
                    if (b == "Photoshop.Application") return 1;
                    return ExtractVersionNumber(b).CompareTo(ExtractVersionNumber(a));
                });

            return list;
        }

        private static int ExtractVersionNumber(string progId)
        {
            int lastDot = progId.LastIndexOf('.');
            if (lastDot < 0 || lastDot == progId.Length - 1)
            {
                return 0;
            }
            int value;
            return int.TryParse(progId.Substring(lastDot + 1), out value) ? value : 0;
        }

        private static void AddUnique(List<string> list, string value)
        {
            foreach (string existing in list)
            {
                if (existing.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            list.Add(value);
        }

        private static object GetProperty(object target, string name)
        {
            return target.GetType().InvokeMember(name, BindingFlags.GetProperty, null, target, null);
        }

        private static string ReadStringProperty(object target, string name)
        {
            try
            {
                object value = GetProperty(target, name);
                return value == null ? "" : Convert.ToString(value);
            }
            catch
            {
                return "";
            }
        }

        private static string GetFolderPath(object doc)
        {
            try
            {
                object path = GetProperty(doc, "Path");
                object fsName = GetProperty(path, "fsName");
                return Convert.ToString(fsName);
            }
            catch
            {
                return "";
            }
        }

        private static string BuildStatusScript()
        {
            return @"
#target photoshop
(function () {
    var version = '';
    try { version = app.version; } catch (versionErr) {}

    if (app.documents.length === 0) {
        return 'COUNT=0\nVERSION=' + version;
    }

    var doc = app.activeDocument;
    var name = '';
    var path = '';
    var full = '';

    try { name = doc.name; } catch (nameErr) {}
    try { path = doc.path.fsName; } catch (pathErr) {}
    try { full = doc.fullName.fsName; } catch (fullErr) {}

    return 'COUNT=1\nVERSION=' + version + '\nNAME=' + name + '\nPATH=' + path + '\nFULL=' + full;
})();";
        }

        private static PhotoshopInfo ParsePhotoshopInfo(string text)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string line in lines)
            {
                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }
                fields[line.Substring(0, eq)] = line.Substring(eq + 1);
            }

            string count = ReadField(fields, "COUNT");
            string version = ReadField(fields, "VERSION");
            if (count == "0")
            {
                return new PhotoshopInfo(false, "已连接，但没有打开文档。", version, "", "", false);
            }

            string name = ReadField(fields, "NAME");
            string sourceFolder = ReadField(fields, "PATH");
            string fullName = ReadField(fields, "FULL");

            if (string.IsNullOrWhiteSpace(sourceFolder) && !string.IsNullOrWhiteSpace(fullName))
            {
                try
                {
                    sourceFolder = Path.GetDirectoryName(fullName);
                }
                catch
                {
                    sourceFolder = "";
                }
            }

            bool hasSourceFolder = !string.IsNullOrWhiteSpace(sourceFolder);
            return new PhotoshopInfo(true, "已连接。", version, name, sourceFolder, hasSourceFolder);
        }

        private static string ReadField(Dictionary<string, string> fields, string key)
        {
            string value;
            return fields.TryGetValue(key, out value) ? value : "";
        }

        private static string ExtractFolder(string message)
        {
            const string marker = "位置：";
            int index = message.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                const string legacyMarker = "Folder: ";
                index = message.LastIndexOf(legacyMarker, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    string legacyRest = message.Substring(index + legacyMarker.Length).Trim();
                    int legacyNewline = legacyRest.IndexOfAny(new[] { '\r', '\n' });
                    return legacyNewline >= 0 ? legacyRest.Substring(0, legacyNewline).Trim() : legacyRest;
                }
            }
            if (index < 0)
            {
                return "";
            }
            string rest = message.Substring(index + marker.Length).Trim();
            int newline = rest.IndexOfAny(new[] { '\r', '\n' });
            return newline >= 0 ? rest.Substring(0, newline).Trim() : rest;
        }

        private static string FormatFailureReason(Exception ex)
        {
            var com = ex as COMException;
            if (com != null)
            {
                string hex = "0x" + com.ErrorCode.ToString("X8");
                if ((uint)com.ErrorCode == 0x8001010A)
                {
                    return "Photoshop 当前忙碌，可能有弹窗未关闭，或正在处理其他任务。请切回 Photoshop 关闭弹窗后重试。(" + hex + ")";
                }

                if ((uint)com.ErrorCode == 0x800401E3)
                {
                    return "没有找到正在运行的 Photoshop。请先打开 Photoshop 后重试。(" + hex + ")";
                }

                if ((uint)com.ErrorCode == 0x80040154)
                {
                    return "系统没有注册 Photoshop 自动化接口。请确认 Photoshop 已正确安装。(" + hex + ")";
                }

                return "Photoshop 自动化接口返回错误：" + com.Message + " (" + hex + ")";
            }

            if (ex is TargetInvocationException && ex.InnerException != null)
            {
                return FormatFailureReason(ex.InnerException);
            }

            return string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;
        }

        private static string JsString(string value)
        {
            if (value == null)
            {
                return "";
            }

            var sb = new StringBuilder();
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        private static string BuildPhotoshopScript(int quality, bool useSourceFolder, string customFolder, bool overwrite)
        {
            string folder = JsString(customFolder);
            string useSource = useSourceFolder ? "true" : "false";
            string overwriteText = overwrite ? "true" : "false";

            return @"
#target photoshop
(function () {
    var SETTINGS = {
        quality: " + quality + @",
        useSourceFolder: " + useSource + @",
        outputFolder: """ + folder + @""",
        overwrite: " + overwriteText + @"
    };

    function fail(message) {
        return ""失败："" + message;
    }

    if (app.documents.length === 0) {
        return fail(""请先在 Photoshop 中打开要导出的文件。"");
    }

    var doc = app.activeDocument;
    var outputFolder;

    if (SETTINGS.useSourceFolder) {
        try {
            outputFolder = doc.path;
        } catch (pathErr) {
            return fail(""当前文档没有原始位置。请先保存文件，或打开一个已经在磁盘上的文件。"");
        }
    } else {
        outputFolder = new Folder(SETTINGS.outputFolder);
        if (!outputFolder.exists) {
            outputFolder.create();
        }
    }

    var originalRulerUnits = app.preferences.rulerUnits;
    app.preferences.rulerUnits = Units.PIXELS;

    var allLayers = [];
    var exportLayers = [];
    var parentMap = {};

    function layerId(layer) {
        return String(layer.id);
    }

    function isBackgroundLayer(layer) {
        try {
            if (doc.backgroundLayer && layer === doc.backgroundLayer) {
                return true;
            }
        } catch (e) {}

        var n = String(layer.name).toLowerCase();
        return n === ""background"" || n === ""\u80cc\u666f"" || n === ""\u80cc\u666f\u56fe\u5c42"";
    }

    function collect(container, parents) {
        for (var i = 0; i < container.layers.length; i++) {
            var layer = container.layers[i];
            allLayers.push(layer);
            parentMap[layerId(layer)] = parents.slice(0);

            if (layer.typename === ""LayerSet"") {
                collect(layer, parents.concat([layer]));
            } else if (!isBackgroundLayer(layer)) {
                exportLayers.push(layer);
            }
        }
    }

    function safeFileName(name) {
        var cleaned = String(name).replace(/[\\\/:*?""<>|]/g, ""_"").replace(/^\s+|\s+$/g, """");
        return cleaned || ""layer"";
    }

    function nextFile(base) {
        var n = 1;
        var candidate = new File(outputFolder.fsName + ""/"" + base + "".jpg"");
        if (SETTINGS.overwrite) {
            if (candidate.exists) {
                candidate.remove();
            }
            return candidate;
        }

        while (candidate.exists) {
            n++;
            candidate = new File(outputFolder.fsName + ""/"" + base + ""_"" + n + "".jpg"");
        }
        return candidate;
    }

    function uniqueName(base, usedNames) {
        var key = base.toLowerCase();
        if (!usedNames[key]) {
            usedNames[key] = 1;
            return base;
        }
        usedNames[key]++;
        return base + ""_"" + usedNames[key];
    }

    function hideAll() {
        for (var i = 0; i < allLayers.length; i++) {
            try {
                allLayers[i].visible = false;
            } catch (e) {}
        }
    }

    function showLayerAndParents(layer) {
        var parents = parentMap[layerId(layer)] || [];
        for (var p = 0; p < parents.length; p++) {
            try {
                parents[p].visible = true;
            } catch (e1) {}
        }
        try {
            layer.visible = true;
        } catch (e2) {}
    }

    function exportJpg(layer, backgroundLayer, usedNames, exportedNames) {
        hideAll();
        showLayerAndParents(backgroundLayer);
        showLayerAndParents(layer);

        var fileBase = uniqueName(safeFileName(layer.name), usedNames);
        var outFile = nextFile(fileBase);

        var jpgOptions = new JPEGSaveOptions();
        jpgOptions.quality = Math.max(1, Math.min(12, SETTINGS.quality));
        jpgOptions.embedColorProfile = true;
        jpgOptions.formatOptions = FormatOptions.STANDARDBASELINE;
        jpgOptions.matte = MatteType.NONE;

        doc.saveAs(outFile, jpgOptions, true, Extension.LOWERCASE);
        exportedNames.push(outFile.name);
    }

    collect(doc, []);

    var backgroundLayer = null;
    try {
        backgroundLayer = doc.backgroundLayer;
    } catch (e1) {}

    if (!backgroundLayer) {
        for (var b = allLayers.length - 1; b >= 0; b--) {
            if (allLayers[b].typename !== ""LayerSet"" && isBackgroundLayer(allLayers[b])) {
                backgroundLayer = allLayers[b];
                break;
            }
        }
    }

    if (!backgroundLayer) {
        for (var fallback = allLayers.length - 1; fallback >= 0; fallback--) {
            if (allLayers[fallback].typename !== ""LayerSet"") {
                backgroundLayer = allLayers[fallback];
                break;
            }
        }
    }

    if (!backgroundLayer) {
        app.preferences.rulerUnits = originalRulerUnits;
        return fail(""没有找到可作为背景的普通图层。"");
    }

    var filteredExportLayers = [];
    for (var fl = 0; fl < exportLayers.length; fl++) {
        if (exportLayers[fl] !== backgroundLayer) {
            filteredExportLayers.push(exportLayers[fl]);
        }
    }
    exportLayers = filteredExportLayers;

    if (exportLayers.length === 0) {
        app.preferences.rulerUnits = originalRulerUnits;
        return fail(""没有找到需要导出的图层。"");
    }

    var visibility = [];
    for (var v = 0; v < allLayers.length; v++) {
        visibility.push({
            layer: allLayers[v],
            visible: allLayers[v].visible
        });
    }

    var usedNames = {};
    var exportedNames = [];
    var exportedCount = 0;
    var errorMessage = """";

    try {
        for (var e = 0; e < exportLayers.length; e++) {
            exportJpg(exportLayers[e], backgroundLayer, usedNames, exportedNames);
            exportedCount++;
        }
    } catch (err) {
        errorMessage = String(err);
    } finally {
        for (var r = visibility.length - 1; r >= 0; r--) {
            try {
                visibility[r].layer.visible = visibility[r].visible;
            } catch (restoreErr) {}
        }
        app.preferences.rulerUnits = originalRulerUnits;
    }

    if (errorMessage) {
        return fail(""导出过程中出错："" + errorMessage + ""\n已导出："" + exportedCount + "" 张\n位置："" + outputFolder.fsName);
    }

    return ""成功：导出完成\n已导出："" + exportedCount + "" 张 JPG\n位置："" + outputFolder.fsName + ""\n文件：\n"" + exportedNames.join(""\n"");
})();";
        }

        private sealed class PhotoshopConnection
        {
            public PhotoshopConnection(object application, string progId)
            {
                Application = application;
                ProgId = progId;
            }

            public object Application { get; private set; }
            public string ProgId { get; private set; }
        }

        private sealed class PhotoshopInfo
        {
            public PhotoshopInfo(bool connected, string status, string version, string documentName, string sourceFolder, bool hasSourceFolder)
            {
                Connected = connected;
                Status = status;
                Version = version;
                DocumentName = documentName;
                SourceFolder = sourceFolder;
                HasSourceFolder = hasSourceFolder;
            }

            public bool Connected { get; private set; }
            public string Status { get; private set; }
            public string Version { get; private set; }
            public string DocumentName { get; private set; }
            public string SourceFolder { get; private set; }
            public bool HasSourceFolder { get; private set; }
        }
    }

    internal static class Theme
    {
        public static readonly Color AppBackground = Color.FromArgb(242, 245, 247);
        public static readonly Color CardBackground = Color.White;
        public static readonly Color Border = Color.FromArgb(214, 222, 226);
        public static readonly Color TitleText = Color.FromArgb(28, 42, 52);
        public static readonly Color MutedText = Color.FromArgb(86, 100, 110);
        public static readonly Color Primary = Color.FromArgb(26, 126, 91);
    }

    internal sealed class CardPanel : Panel
    {
        public CardPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedRect(rect, 8))
            using (SolidBrush fill = new SolidBrush(Theme.CardBackground))
            using (Pen border = new Pen(Theme.Border))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
            base.OnPaint(e);
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class GradientPanel : Panel
    {
        public GradientPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        public int Radius { get; set; }
        public Color StartColor { get; set; }
        public Color EndColor { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedRect(rect, Radius))
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, StartColor, EndColor, LinearGradientMode.Horizontal))
            {
                e.Graphics.FillPath(brush, path);
            }
            base.OnPaint(e);
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            int d = Math.Max(1, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal static class LogoFactory
    {
        private const string ResourceName = "PSJpgExporterApp.logo.png";

        public static Bitmap CreateBitmap(int size)
        {
            Bitmap resourceLogo = TryLoadResourceLogo(size);
            if (resourceLogo != null)
            {
                return resourceLogo;
            }

            return CreateFallbackBitmap(size);
        }

        private static Bitmap TryLoadResourceLogo(int size)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    using (Image source = Image.FromStream(stream))
                    {
                        return ResizeSquare(source, size);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static Bitmap ResizeSquare(Image source, int size)
        {
            var bitmap = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                Rectangle src = CropSquare(source.Width, source.Height);
                g.DrawImage(source, new Rectangle(0, 0, size, size), src, GraphicsUnit.Pixel);
            }
            return bitmap;
        }

        private static Rectangle CropSquare(int width, int height)
        {
            int side = Math.Min(width, height);
            return new Rectangle((width - side) / 2, (height - side) / 2, side, side);
        }

        private static Bitmap CreateFallbackBitmap(int size)
        {
            var bitmap = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                Rectangle rect = new Rectangle(1, 1, size - 2, size - 2);
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, Color.FromArgb(12, 32, 54), Color.FromArgb(26, 126, 91), 45f))
                using (GraphicsPath path = RoundedRect(rect, 12))
                {
                    g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(Color.FromArgb(180, 255, 255, 255), 2))
                {
                    g.DrawLine(pen, size * 0.22f, size * 0.68f, size * 0.78f, size * 0.32f);
                    g.DrawEllipse(pen, size * 0.18f, size * 0.18f, size * 0.22f, size * 0.22f);
                    g.DrawEllipse(pen, size * 0.60f, size * 0.56f, size * 0.22f, size * 0.22f);
                }

                using (Font font = new Font("Segoe UI", size / 4.6f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString("PS", font, textBrush, size * 0.18f, size * 0.34f);
                    g.DrawString("JPG", new Font("Segoe UI", size / 6.4f, FontStyle.Bold, GraphicsUnit.Pixel), textBrush, size * 0.42f, size * 0.58f);
                }
            }
            return bitmap;
        }

        public static Icon CreateIcon()
        {
            Bitmap bitmap = CreateBitmap(64);
            IntPtr handle = bitmap.GetHicon();
            return Icon.FromHandle(handle);
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
