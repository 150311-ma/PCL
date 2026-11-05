using System;
using System.IO;
using PCL.Modular.Abstractions;
using PCL.Modular.Contracts;

namespace Pcv.Stat
{
    /// <summary>
    /// 用了几次、从哪天开始用的、这次开了多久。
    /// 只记本地数字，不上传，不发网络请求。
    /// </summary>
    public sealed class Entry : IPclModule
    {
        private const string KeyStarts = "StatStarts";
        private const string KeyFirst = "StatFirstSeen";
        private const string KeySeconds = "StatSeconds";
        private const string KeyLastStart = "StatLastStart";

        private IModuleContext _context;
        private ISettingsService _settings;
        private DateTime _sessionStart;

        public void OnLoad(IModuleContext context)
        {
            _context = context;
            if (!context.Services.TryGet(out _settings))
            {
                context.Log.Warn("没有配置服务，统计数据只在这一次运行里有效");
            }
            _sessionStart = DateTime.Now;
        }

        public void OnStart()
        {
            var starts = Read(KeyStarts) + 1;
            Write(KeyStarts, starts);

            var first = Read(KeyFirst);
            if (first <= 0)
            {
                first = Now();
                Write(KeyFirst, first);
            }

            var last = Read(KeyLastStart);
            Write(KeyLastStart, Now());

            var days = Math.Max(0, (Now() - first) / 86400);
            var totalHours = Read(KeySeconds) / 3600.0;

            var sinceLast = last <= 0
                ? "第一次用"
                : $"上次打开是 {DateTimeOffset.FromUnixTimeSeconds(last).LocalDateTime:MM-dd HH:mm}";

            _context.Log.Info($"第 {starts} 次打开　用了 {days} 天　累计 {totalHours:0.#} 小时　{sinceLast}");
        }

        public void OnStop()
        {
            var seconds = (int)Math.Max(0, (DateTime.Now - _sessionStart).TotalSeconds);
            Write(KeySeconds, Read(KeySeconds) + seconds);
            _context.Log.Info($"这次开了 {(seconds / 60.0):0.#} 分钟，已记下");
        }

        private int Read(string key) => _settings == null ? 0 : _settings.GetValue(key, 0);

        private void Write(string key, int value)
        {
            if (_settings == null) return;
            _settings.SetValue(key, value);
        }

        private static int Now() => (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
