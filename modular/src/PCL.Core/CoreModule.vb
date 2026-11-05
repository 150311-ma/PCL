Imports System.IO
Imports PCL.Modular.Abstractions
Imports PCL.Modular.Contracts

Namespace Global.PCL.Core

    ''' <summary>
    ''' PCL 基础模块。
    '''
    ''' 承载从上游 Modules/Base 抽出的、可以脱离主程序独立存在的部分：
    '''   · 路径约定（PclPaths）
    '''   · 配置持久化（SettingsStore → ISettingsService）
    '''   · 颜色运算（MyColor）
    ''' 不承载任何 UI 或启动逻辑，那些属于其它模块。
    ''' </summary>
    Public Class CoreModule
        Implements IPclModule

        Private _log As ILogger
        Private _settings As SettingsStore
        Private _paths As PclPaths

        Public Sub OnLoad(context As IModuleContext) Implements IPclModule.OnLoad
            _log = context.Log

            Dim dataDirectory = Path.Combine(context.Host.BaseDirectory, "PCLData")
            _paths = New PclPaths(context.Host.BaseDirectory, dataDirectory)
            If Not _paths.EnsureDirectories() Then
                _log.Warn($"数据目录创建失败，配置将无法持久化：{dataDirectory}")
            End If

            _settings = New SettingsStore(_paths.SettingsFile)
            If _settings.LastError IsNot Nothing Then
                _log.Warn($"配置读取有异常，已回退为空配置：{_settings.LastError}")
            End If

            context.Services.Register(Of ISettingsService)(_settings)
            context.Data("paths") = _paths

            _log.Info($"基础模块已装载：数据目录 {_paths.DataDirectory}，配置 {_paths.SettingsFile}")
        End Sub

        Public Sub OnStart() Implements IPclModule.OnStart
            _log.Info("基础模块就绪")
        End Sub

        Public Sub OnStop() Implements IPclModule.OnStop
            If _settings Is Nothing Then Return
            If Not _settings.HasUnsavedChanges Then Return
            If _settings.Save() Then
                _log.Info("配置已落盘")
            Else
                _log.Error("配置落盘失败：" & _settings.LastError)
            End If
        End Sub

        Public ReadOnly Property Paths As PclPaths
            Get
                Return _paths
            End Get
        End Property

    End Class

End Namespace
