using System;
using System.IO;
using System.Windows;
using PCL.Modular.Host;

namespace PCL.Shell
{
    /// <summary>
    /// 壳程序：不写任何业务代码，只做三件事——
    /// 把加载器跑起来、把状态显示出来、退出时按逆序关掉模块。
    /// </summary>
    public partial class App : Application
    {
        private ModuleHost _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // 命令行自检：提示词能不能取到、能不能进剪贴板（界面之外也能验）
            if (Array.IndexOf(e.Args, "--copy-prompt") >= 0)
            {
                var prompt = PromptLibrary.Load();
                var ok = ClipboardWriter.TrySetText(prompt, 6);
                Console.WriteLine($"promptChars={prompt.Length} clipboard={ok}");
                Shutdown(ok && prompt.Length > 500 ? 0 : 2);
                return;
            }
            _host = new ModuleHost(new ModuleHostOptions
            {
                ModulesDirectory = Path.Combine(baseDirectory, "modules"),
                LogFile = Path.Combine(baseDirectory, "logs", "modules.log"),
                EchoToConsole = false,
                HostName = "PAINCHARNWAV",
                HostVersion = "1.0.0"
            });

            ModuleLoadReport report;
            try
            {
                report = _host.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show("模块加载过程异常终止：\n" + ex, "PAINCHARNWAV", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            // 主题必须赶在窗口构造前应用：MainWindow 的构造函数会把当前主题读进下拉框与顶栏渐变
            var themeIndex = Array.IndexOf(e.Args, "--theme");
            if (themeIndex >= 0 && themeIndex + 1 < e.Args.Length &&
                _host.Services.TryGet(out PCL.Modular.Contracts.IThemeService themeService))
            {
                themeService.Apply(e.Args[themeIndex + 1]);
            }

            var window = new MainWindow(_host, report);
            MainWindow = window;

            // 给离屏渲染用：--view add 直接停在"加一个包"那一页
            var viewIndex = Array.IndexOf(e.Args, "--view");
            if (viewIndex >= 0 && viewIndex + 1 < e.Args.Length && e.Args[viewIndex + 1] == "add")
                window.ShowAddViewForRender();

            // 离屏渲染：给自动化验证用，不弹窗、不需要人盯着，也能确认界面真的画出来了
            var renderIndex = Array.IndexOf(e.Args, "--render");
            if (renderIndex >= 0)
            {
                var target = renderIndex + 1 < e.Args.Length
                    ? e.Args[renderIndex + 1]
                    : Path.Combine(baseDirectory, "preview.png");
                var width = 1080;
                var height = 720;
                var sizeIndex = Array.IndexOf(e.Args, "--size");
                if (sizeIndex >= 0 && sizeIndex + 1 < e.Args.Length)
                {
                    var parts = e.Args[sizeIndex + 1].Split('x');
                    if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
                    {
                        width = w;
                        height = h;
                    }
                }
                try
                {
                    RenderToFile(window, target, width, height);
                    Console.WriteLine("已渲染：" + target);
                    Shutdown(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("渲染失败：" + ex);
                    Shutdown(2);
                }
                return;
            }

            window.Show();
        }

        private static void RenderToFile(Window window, string path, int width, int height)
        {
            var content = (FrameworkElement)window.Content;
            window.Content = null;
            content.Width = width;
            content.Height = height;
            content.Measure(new Size(width, height));
            content.Arrange(new Rect(0, 0, width, height));
            content.UpdateLayout();

            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(width, height, 96, 96,
                System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(content);

            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            using (var stream = File.Create(path)) encoder.Save(stream);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}
