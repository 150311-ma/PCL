using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using PCL.Modular.Abstractions;
using PCL.Modular.Host;
using PCL.Modular.Runtime;

namespace PCL.Shell
{
    /// <summary>
    /// 壳里的"框架运行入口"。
    ///
    /// 两种框架走同一个口子出去：
    ///   · 程序集型（.NET DLL）→ 由 ModuleHost 在进程内加载，窗口是 FrameworkWindow；
    ///   · 进程型（py / node / exe）→ 由 ProcessFramework 起子进程，窗口是 ProcessWindow。
    /// 进程型不经过 ModuleHost，因为它压根不是 .NET 程序集。
    /// </summary>
    public sealed class ModuleHostAccessor
    {
        private readonly ModuleHost _host;
        private readonly Dictionary<string, ProcessWindow> _windows =
            new Dictionary<string, ProcessWindow>(StringComparer.OrdinalIgnoreCase);

        public ModuleHostAccessor(ModuleHost host)
        {
            _host = host;
            BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }

        public string BaseDirectory { get; }
        public string ModulesDirectory => _host.ModulesDirectory;
        public ILogger Log => _host.Log;

        public static ModuleManifest ReadManifest(string directory)
        {
            try
            {
                var path = Path.Combine(directory, "module.json");
                if (!File.Exists(path)) return null;
                var manifest = JsonConvert.DeserializeObject<ModuleManifest>(File.ReadAllText(path));
                if (manifest != null) manifest.DirectoryPath = directory;
                return manifest;
            }
            catch
            {
                return null;
            }
        }

        public bool IsRunning(string moduleId) => FrameworkRegistry.Find(moduleId) != null;

        /// <summary>启动一个进程型框架并给它一个窗口；已经在跑就把窗口拉到前面。</summary>
        public void RunProcessFramework(ModuleManifest manifest, string directory, Window owner)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));

            if (_windows.TryGetValue(manifest.Id, out var existing) && existing.IsVisible)
            {
                existing.Activate();
                return;
            }

            var dataDirectory = Path.Combine(BaseDirectory, "PCLData", "frameworks", manifest.Id);
            Directory.CreateDirectory(dataDirectory);

            var framework = new ProcessFramework(manifest, Log, dataDirectory);
            framework.Start();
            Log.Info($"[proc] {manifest.Id} 进程已启动，准备开窗口");

            var window = new ProcessWindow(framework);
            Log.Info("[proc] 窗口对象建好了");
            if (owner != null && owner.IsVisible && !ReferenceEquals(owner, window)) window.Owner = owner;

            _windows[manifest.Id] = window;
            window.Closed += (s, e) => _windows.Remove(manifest.Id);
            window.Show();
            Log.Info($"[proc] 窗口已显示（Visible={window.IsVisible}）");
        }
    }
}
