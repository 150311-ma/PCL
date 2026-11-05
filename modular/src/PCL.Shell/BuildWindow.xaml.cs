using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PCL.Modular.Abstractions;

namespace PCL.Shell
{
    /// <summary>
    /// 构建脚本的运行窗口：左边不用管，它就是个带进度和"装进 PCV"按钮的日志窗。
    /// </summary>
    public partial class BuildWindow : Window
    {
        private readonly ScriptBuilder _builder;
        private readonly string _scriptPath;
        private readonly Action<ScriptBuilder.Result> _onFinished;
        private readonly StringBuilder _buffer = new StringBuilder();

        public BuildWindow(ScriptBuilder builder, string scriptPath, Action<ScriptBuilder.Result> onFinished)
        {
            _builder = builder;
            _scriptPath = scriptPath;
            _onFinished = onFinished;
            InitializeComponent();

            LabScript.Text = System.IO.Path.GetFileName(scriptPath);
            Title = "构建框架包 · PCV";
            Loaded += (s, e) =>
            {
                GlassHelper.TryEnableRoundedCorners(this);
                Start();
            };
        }

        private void Start()
        {
            LabStatus.Text = "正在跑脚本…";
            System.Threading.Tasks.Task.Run(() =>
            {
                var result = _builder.Run(_scriptPath, line => Dispatcher.Invoke(() => Append(line)));
                Dispatcher.Invoke(() => Finish(result));
            });
        }

        private void Append(string line)
        {
            _buffer.AppendLine(line);
            if (_buffer.Length > 300000) _buffer.Remove(0, _buffer.Length - 150000);
            Output.Text = _buffer.ToString();
            Scroller.ScrollToEnd();
        }

        private void Finish(ScriptBuilder.Result result)
        {
            Append("");
            Append("── 结束 ──");
            Append(result.Message);
            if (result.ExitCode != 0) Append("工作目录（脚本和中间产物都在这）：" + result.WorkDirectory);

            LabStatus.Text = result.Message;
            if (result.Success)
            {
                Dot.Fill = new SolidColorBrush(Color.FromRgb(0x3D, 0xDC, 0x84));
                BtnInstall.IsEnabled = true;
                BtnInstall.Content = "装进 PCV";
            }
            else
            {
                Dot.Fill = new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49));
                BtnInstall.IsEnabled = false;
            }

            _onFinished?.Invoke(result);
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            BtnInstall.IsEnabled = false;
            BtnInstall.Content = "安装中…";
            InstallRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler InstallRequested;

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }
    }
}
