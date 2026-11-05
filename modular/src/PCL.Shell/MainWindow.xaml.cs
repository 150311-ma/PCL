using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PCL.Modular.Abstractions;
using PCL.Modular.Contracts;
using PCL.Modular.Host;
using PCL.Modular.Packages;

namespace PCL.Shell
{
    /// <summary>
    /// 唯一的一页：拖包 → 装 → 立刻可用。
    ///
    /// 界面之外没有第二个页面、没有侧边栏、没有设置页。
    /// 能力全部来自 modules 目录，壳只负责"装载、展示、装包、卸载"。
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ModuleHost _host;
        private readonly PackageInstaller _installer;
        private readonly IThemeService _theme;
        private readonly ObservableCollection<FrameworkCard> _cards = new ObservableCollection<FrameworkCard>();
        private ModuleLoadReport _report;

        public MainWindow(ModuleHost host, ModuleLoadReport report)
        {
            _host = host;
            _report = report;
            InitializeComponent();

            _installer = new PackageInstaller(host.ModulesDirectory, host.Log);
            host.Services.TryGet(out _theme);

            ListModules.ItemsSource = _cards;
            LabVersion.Text = host.Host.Version;
            RefreshModules();

            Loaded += (s, e) => GlassHelper.TryEnableRoundedCorners(this);
        }

        // ── 列表 ────────────────────────────────────────────────────────────

        private void RefreshModules()
        {
            _cards.Clear();
            var installed = _installer.ListInstalled().ToDictionary(m => m.Id, m => m, StringComparer.OrdinalIgnoreCase);

            foreach (var entry in _report.Entries.OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase))
            {
                installed.TryGetValue(entry.Id, out var record);
                _cards.Add(new FrameworkCard
                {
                    Id = entry.Id,
                    Name = entry.Manifest?.DisplayName ?? record?.Name ?? entry.Id,
                    Version = entry.Manifest?.Version ?? record?.Version ?? "?",
                    State = StateText(entry.State),
                    Detail = entry.Error ?? entry.Manifest?.Description ?? "由内置模块提供",
                    Initial = Initial(entry.Id),
                    CanUninstall = !string.Equals(entry.Id, "pcl.core", StringComparison.OrdinalIgnoreCase)
                });
            }

            LabCount.Text = $"{_report.StartedCount} 个运行中 / 共 {_report.Entries.Count} 个";
            LabEmpty.Visibility = _cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            LabFooter.Text = "模块目录 modules\\　·　日志 logs\\modules.log";
        }

        /// <summary>用模块 Id 的最后一段取首字母：pcl.core → C，pcl.theme → T，比一排 P 好认。</summary>
        private static string Initial(string moduleId)
        {
            if (string.IsNullOrWhiteSpace(moduleId)) return "?";
            var tail = moduleId.Split('.').LastOrDefault() ?? moduleId;
            var first = tail.Trim().FirstOrDefault();
            return char.IsLetter(first) ? char.ToUpperInvariant(first).ToString() : "P";
        }

        private static string StateText(ModuleState state)
        {
            switch (state)
            {
                case ModuleState.Started: return "运行中";
                case ModuleState.Loaded: return "已加载";
                case ModuleState.Stopped: return "已停止";
                case ModuleState.Skipped: return "已跳过";
                case ModuleState.Failed: return "失败";
                default: return state.ToString();
            }
        }

        private void SetStatus(string message, bool isError = false)
        {
            LabStatus.Text = message;
            LabStatus.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B))
                : new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x63));
        }

        // ── 拖放 ────────────────────────────────────────────────────────────

        private static bool IsPackage(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            return files != null && files.Length == 1 && IsPackagePath(files[0]);
        }

        private static bool IsPackagePath(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".pclpkg" || extension == ".zip";
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            var accepted = IsPackage(e);
            e.Effects = accepted ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;

            DropFrame.Stroke = accepted
                ? (Brush)FindResource("ColorBrush3")
                : new SolidColorBrush(Color.FromRgb(0x8F, 0xA9, 0xC6));
            DropFrame.StrokeThickness = accepted ? 2.2 : 1.6;
            DropFrame.Fill = new SolidColorBrush(Color.FromArgb(accepted ? (byte)0x66 : (byte)0x33, 0xFF, 0xFF, 0xFF));
            LabDropTitle.Text = accepted ? "松手就装" : "只接受 .pclpkg / .zip";
        }

        private void Window_DragLeave(object sender, DragEventArgs e) => ResetDropZone();

        private void Window_Drop(object sender, DragEventArgs e)
        {
            ResetDropZone();
            if (!IsPackage(e)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            InstallPackage(files[0]);
        }

        private void ResetDropZone()
        {
            DropFrame.Stroke = new SolidColorBrush(Color.FromRgb(0x8F, 0xA9, 0xC6));
            DropFrame.StrokeThickness = 1.6;
            DropFrame.Fill = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
            LabDropTitle.Text = "拖入 .pclpkg 文件";
        }

        // ── 安装 ────────────────────────────────────────────────────────────

        private void BtnChoose_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择功能框架包",
                Filter = "功能框架包 (*.pclpkg;*.zip)|*.pclpkg;*.zip|所有文件 (*.*)|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) == true) InstallPackage(dialog.FileName);
        }

        private void InstallPackage(string path)
        {
            // 1. 先只读检查，问题在动盘之前就说清楚
            var inspection = _installer.Inspect(path);
            if (!inspection.IsValid)
            {
                SetStatus("装不了：" + string.Join("；", inspection.Errors.Take(2)), true);
                return;
            }

            // 2. 已存在就明确问一次，绝不悄悄覆盖
            var target = Path.Combine(_installer.ModulesDirectory, inspection.Manifest.Id);
            var overwrite = false;
            if (Directory.Exists(target))
            {
                var answer = MessageBox.Show(this,
                    $"已经装过 {inspection.Manifest.DisplayName} 了。\n" +
                    $"用包里的 {inspection.Manifest.Version} 覆盖它吗？\n" +
                    "旧版本会保留在 modules\\.backup，装坏了能拿回来。",
                    "确认覆盖", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (answer != MessageBoxResult.OK)
                {
                    SetStatus("已取消覆盖安装");
                    return;
                }
                overwrite = true;
            }

            SetStatus($"正在安装 {inspection.Manifest.DisplayName}…");

            // 3. 提交
            var result = _installer.Install(path, overwrite);
            if (!result.Success)
            {
                SetStatus(result.Message, true);
                return;
            }

            // 4. 重载模块表，让新框架立刻可用
            _report = _host.ReloadAll();
            RefreshModules();

            var loaded = _report.Entries.FirstOrDefault(m => string.Equals(m.Id, result.ModuleId, StringComparison.OrdinalIgnoreCase));
            if (loaded != null && loaded.State == ModuleState.Started)
                SetStatus($"{result.Message}，已生效。");
            else if (loaded != null)
                SetStatus($"{result.Message}，但加载失败：{loaded.Error}", true);
            else
                SetStatus($"{result.Message}。");
        }

        // ── 卸载 ────────────────────────────────────────────────────────────

        private void BtnUninstall_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element) || !(element.Tag is string moduleId)) return;

            var answer = MessageBox.Show(this,
                $"确定卸载 {moduleId} 吗？\n\n" +
                "卸载会把它移出模块目录；已经加载进内存的那一份要等重启才彻底消失。",
                "卸载功能框架", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.OK) return;

            if (!_installer.Uninstall(moduleId, out var message))
            {
                SetStatus(message, true);
                return;
            }

            _report = _host.ReloadAll();
            RefreshModules();
            SetStatus($"{moduleId} 已卸载。{message}");
        }

        // ── 窗口 ────────────────────────────────────────────────────────────

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                return;
            }
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void BtnMin_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        public sealed class FrameworkCard
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public string State { get; set; }
            public string Detail { get; set; }
            public string Initial { get; set; }
            public bool CanUninstall { get; set; }
        }
    }
}
