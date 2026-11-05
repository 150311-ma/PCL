using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Modular.Abstractions
{
    /// <summary>
    /// module.json 的映射。加载器只认这份清单，不扫描程序集里的“魔法约定”。
    /// </summary>
    public sealed class ModuleManifest
    {
        /// <summary>全局唯一标识，建议小写点分：pcl.core / pcl.theme。</summary>
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; } = "1.0.0";
        /// <summary>程序集文件名，相对于模块目录。</summary>
        public string Assembly { get; set; }
        /// <summary>入口类型的全名；留空则自动取程序集中第一个 IPclModule 实现。</summary>
        public string Entry { get; set; }
        /// <summary>依赖的模块 Id。</summary>
        public List<string> Dependencies { get; set; } = new List<string>();
        /// <summary>同层级内的加载顺序，小的先加载。</summary>
        public int LoadPriority { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        /// <summary>可选标签，仅供界面分组，不参与加载逻辑。</summary>
        public List<string> Tags { get; set; } = new List<string>();
        /// <summary>要求的最低宿主版本，语义化版本比较。</summary>
        public string MinHostVersion { get; set; }

        /// <summary>清单文件所在目录，由加载器在发现阶段填入。</summary>
        public string DirectoryPath { get; set; }

        public string DisplayName => string.IsNullOrEmpty(Name) ? Id : Name;
        public string DependencyText => Dependencies == null || Dependencies.Count == 0 ? "-" : string.Join(", ", Dependencies);

        public override string ToString() => $"{Id}@{Version}";

        /// <summary>校验清单自身的完整性，返回错误列表（空表示通过）。</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("缺少 id");
            else if (Id.Count(c => c == '.') == 0) errors.Add($"id 建议使用点分命名：{Id}");
            if (string.IsNullOrWhiteSpace(Assembly)) errors.Add("缺少 assembly");
            if (Dependencies != null && Dependencies.Contains(Id)) errors.Add("模块不能依赖自身");
            if (Dependencies != null && Dependencies.Distinct().Count() != Dependencies.Count)
                errors.Add("dependencies 中存在重复项");
            if (!string.IsNullOrWhiteSpace(MinHostVersion) && !IsVersionAtLeast(HostInfoVersion, MinHostVersion))
                errors.Add($"需要宿主版本 >= {MinHostVersion}，当前 {HostInfoVersion}");
            return errors;
        }

        /// <summary>由加载器注入，用于 MinHostVersion 校验。</summary>
        public static string HostInfoVersion { get; set; } = "1.0.0";

        private static bool IsVersionAtLeast(string current, string required)
        {
            if (!System.Version.TryParse(Normalize(current), out var c)) return true;
            if (!System.Version.TryParse(Normalize(required), out var r)) return true;
            return c >= r;
        }

        private static string Normalize(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "0.0.0";
            var parts = v.Split('-')[0].Split('.');
            while (parts.Length < 2) parts = parts.Concat(new[] { "0" }).ToArray();
            return string.Join(".", parts.Take(4));
        }
    }
}
