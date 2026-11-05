# PCL 多模块化改造

把 PCL（Plain Craft Launcher 2，VB.NET / .NET Framework 4.8 / WPF）从**单程序集全局状态**
改成**模块加载器 + 契约 + 独立模块**的结构。

本目录是改造后的骨架，**不动**仓库里原有的 `Plain Craft Launcher 2/` 单体工程：
它作为对照实现保留，随时可以和上游 `Meloong-Git/PCL` 同步，不会因为改造而失去追溯能力。

## 一、为什么不能"直接把文件搬到新工程"

PCL 原本是一个程序集里的全局状态机：

- 页面直接读 `ModMain`、`States`、`FrmMain.PanXXX`；
- 模块之间互相 `Public Module` 直接调用，没有接口；
- 主题靠直接改 `Application.Current.Resources` 并抓住 `FrmMain.PanTitle`。

所以"拆"不是挪文件，而是**先立契约，再让代码只依赖契约**。本次改造按这个顺序落地：
先做契约与加载器，再把可以独立的部分（基础功能 / 主题 / 微软登录）真正编成独立程序集，
最后才动 UI（UI 依赖面最大，见各 UI 仓库的 `MIGRATION.md`）。

## 二、目录

```
modular/
├── PCL.Modular.sln
├── src/
│   ├── PCL.Modular.Abstractions/   契约：IPclModule、清单、服务表、跨模块接口
│   ├── PCL.Modular.Host/           模块加载器（本目录的主角）
│   ├── PCL.Modular.Cli/            加载器验证器（含故障注入用例）
│   ├── PCL.Core/                   基础模块：路径、配置、MyColor
│   ├── PCL.Theme/                  主题模块：HSL 调色 + 资源注入
│   ├── PCL.Launch.Microsoft/       微软登录模块：设备码 → XBL → XSTS → MC
│   └── PCL.Shell/                  壳程序：只有窗口和加载逻辑，没有业务代码
└── verify.cmd                      一键构建 + 两组验证
```

模块之间**不互相引用程序集**：需要别人的能力，就从 `IModuleContext.Services` 按接口取。
跨模块接口统一放在 `PCL.Modular.Abstractions/Contracts.cs`。

## 三、模块清单

| 模块 Id | 工程 | 依赖 | 对应上游 | 状态 |
| --- | --- | --- | --- | --- |
| `pcl.core` | PCL.Core.vbproj | — | `Modules/Base`（路径、MyColor）、`Settings` | ✅ 可编译可加载 |
| `pcl.theme` | PCL.Theme.vbproj | pcl.core | `ModSecret` 主题区域 + `Application.xaml` 颜色表 | ✅ 可编译可加载 |
| `pcl.launch.microsoft` | PCL.Launch.Microsoft.vbproj | pcl.core | `ModLaunch.McLoginMsStart` / `MsLoginStep1~6` | ✅ 可编译可加载 |
| `pcl.ui.*` | 各自独立仓库 | pcl.core / pcl.theme / pcl.ui.controls | `Pages/*`、`Controls/` | 🚧 源码已归位，适配中 |

UI 模块在独立仓库里，见 `ARCHITECTURE.md` 的仓库表。

## 四、构建与验证

```bat
cd modular
dotnet build PCL.Modular.sln
```

一键验证（构建 + 故障注入 + 真实模块加载）：

```bat
modular\verify.cmd
```

单独跑：

```bat
:: 真实模块
src\PCL.Modular.Cli\bin\Debug\PCL.Modular.Cli.exe --modules src\PCL.Shell\bin\Debug\modules

:: 故障注入：造出坏 JSON / 缺 dll / OnLoad 抛异常 / 循环依赖 / 重复 Id，确认加载器会判红
src\PCL.Modular.Cli\bin\Debug\PCL.Modular.Cli.exe --inject-fault
```

界面（离屏渲染，不需要人盯着看）：

```bat
src\PCL.Shell\bin\Debug\PCL.Shell.exe --render preview.png --theme violet
```

## 五、加载器规则（刻意做成可判定）

1. **只认 `module.json`**，不扫程序集特性，不猜约定；
2. **依赖拓扑排序**，循环依赖整组判失败，不做"尽力加载"；
3. **单个模块失败只影响它和它的下游**：下游标记为"已跳过"并写明是哪个依赖挂了；
4. **重复 Id 只保留最高版本**，其余显式记失败（不允许悄悄生效）；
5. **`OnStart` 逆序 `OnStop`**，退出时按依赖反向收尾。

这些规则每一条都有对应的注入用例，`--inject-fault` 会逐条验证。
