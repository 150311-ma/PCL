using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCL.Modular.Contracts;
using PCL.Modular.Host;

namespace PCL.Shell
{
    public partial class MainWindow : Window
    {
        private readonly ModuleHost _host;
        private readonly IThemeService _theme;
        private readonly IMicrosoftAuthService _msAuth;

        public MainWindow(ModuleHost host, ModuleLoadReport report)
        {
            _host = host;
            InitializeComponent();

            host.Services.TryGet(out _theme);
            host.Services.TryGet(out _msAuth);

            FillModules(report);
            FillThemes();
            ShowMsState();

            LabFooter.Text = "日志 logs\\modules.log　·　模块目录 modules\\　·　壳程序不含任何业务代码，功能全部来自 modules 下的模块";

            if (GridModules.Items.Count > 0) GridModules.SelectedIndex = 0;
        }

        // ── 模块表 ──────────────────────────────────────────────────────────

        private void FillModules(ModuleLoadReport report)
        {
            GridModules.ItemsSource = report.Entries.Select(entry => new ModuleRow
            {
                Name = entry.Manifest?.DisplayName ?? "?",
                Id = entry.Id,
                Version = entry.Manifest?.Version ?? "-",
                State = StateText(entry.State),
                Elapsed = entry.LoadMilliseconds + entry.StartMilliseconds + " ms",
                Detail = entry.Error == null
                    ? entry.Manifest?.Description ?? ""
                    : "加载异常：" + entry.Error
            }).ToList();

            LabSummary.Text = $"已启动 {report.StartedCount} · 失败 {report.FailedCount} · 跳过 {report.SkippedCount}";
            LabSummary.Foreground = report.IsHealthy
                ? (Brush)FindResource("ColorBrushGray3")
                : new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
        }

        private static string StateText(PCL.Modular.Abstractions.ModuleState state)
        {
            switch (state)
            {
                case PCL.Modular.Abstractions.ModuleState.Started: return "运行中";
                case PCL.Modular.Abstractions.ModuleState.Loaded: return "已加载";
                case PCL.Modular.Abstractions.ModuleState.Stopped: return "已停止";
                case PCL.Modular.Abstractions.ModuleState.Skipped: return "已跳过";
                case PCL.Modular.Abstractions.ModuleState.Failed: return "失败";
                default: return state.ToString();
            }
        }

        private void GridModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(GridModules.SelectedItem is ModuleRow row))
            {
                LabDetail.Text = "";
                return;
            }
            LabDetail.Text = $"{row.Name}　{row.Id}@{row.Version}　[{row.State}]　{row.Elapsed}\n" +
                             (string.IsNullOrWhiteSpace(row.Detail) ? "该模块没有提供说明。" : row.Detail);
        }

        // ── 主题 ────────────────────────────────────────────────────────────

        private void FillThemes()
        {
            if (_theme == null)
            {
                ComboTheme.IsEnabled = false;
                LabTheme.Text = "主题模块未加载（检查 modules/pcl.theme）";
                return;
            }

            ComboTheme.ItemsSource = _theme.Themes;
            ComboTheme.DisplayMemberPath = "Name";
            ComboTheme.SelectedItem = _theme.Themes.FirstOrDefault(t => t.Id == _theme.Current.Id);
            RefreshThemeSurface();
        }

        private void ComboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_theme == null || !(ComboTheme.SelectedItem is ThemeInfo theme)) return;
            if (!_theme.Apply(theme.Id)) return;
            RefreshThemeSurface();
        }

        private void RefreshThemeSurface()
        {
            if (_theme == null) return;

            LabTheme.Text = $"{_theme.Current.Name}　色相 {_theme.Current.Hue}　饱和度 {_theme.Current.Saturation}　亮度偏移 {_theme.Current.LightAdjust}";

            var stops = _theme.GetTopbarGradient();
            if (stops == null || stops.Count == 0) return;
            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
            foreach (var stop in stops)
            {
                brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(stop.Color), stop.Offset));
            }
            PanTitle.Background = brush;
        }

        // ── 微软登录 ────────────────────────────────────────────────────────

        private void ShowMsState()
        {
            if (_msAuth == null)
            {
                LabMsState.Text = "微软登录模块未加载";
                BtnLogin.IsEnabled = false;
                return;
            }

            if (!_msAuth.IsConfigured)
            {
                LabMsState.Text = "未配置 ClientId";
                LabMsHint.Text = "开源版不含内置 ClientId：设置环境变量 PCL_MS_CLIENT_ID 后重启壳程序，按钮才会真正发起登录。";
                BtnLogin.IsEnabled = true; // 允许点，以便验证错误提示本身是对的
                return;
            }

            var cached = _msAuth.CachedAccount;
            LabMsState.Text = cached == null ? "当前没有已登录账号" : $"已登录：{cached.Name}";
            LabMsHint.Text = cached == null ? "" : $"令牌有效期至 {cached.ExpiresAt.LocalDateTime:yyyy-MM-dd HH:mm}";
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (_msAuth == null) return;

            BtnLogin.IsEnabled = false;
            LabUserCode.Text = "";
            try
            {
                if (!_msAuth.IsConfigured)
                {
                    LabMsState.Text = "无法登录：未配置 ClientId";
                    LabMsHint.Text = "这是预期行为——开源版不含 ClientId。设置环境变量 PCL_MS_CLIENT_ID 后重启即可。";
                    return;
                }

                LabMsState.Text = "正在申请设备码…";
                var deviceCode = await _msAuth.RequestDeviceCodeAsync(CancellationToken.None);
                LabUserCode.Text = deviceCode.UserCode;
                LabMsState.Text = $"请在浏览器打开 {deviceCode.VerificationUri}";
                LabMsHint.Text = "等待授权中…授权完成后窗口会自动继续。";

                var progress = new Progress<string>(text => LabMsHint.Text = text);
                var account = await _msAuth.CompleteLoginAsync(deviceCode, progress, CancellationToken.None);

                LabMsState.Text = $"登录成功：{account.Name}";
                LabMsHint.Text = $"UUID {account.Uuid}　令牌有效期至 {account.ExpiresAt.LocalDateTime:yyyy-MM-dd HH:mm}";
                LabUserCode.Text = "";
            }
            catch (Exception ex)
            {
                LabMsState.Text = "登录失败";
                LabMsHint.Text = ex.Message;
            }
            finally
            {
                BtnLogin.IsEnabled = true;
            }
        }

        private sealed class ModuleRow
        {
            public string Name { get; set; }
            public string Id { get; set; }
            public string Version { get; set; }
            public string State { get; set; }
            public string Elapsed { get; set; }
            public string Detail { get; set; }
        }
    }
}
