Imports PCL.Modular.Abstractions
Imports PCL.Modular.Contracts

Namespace Global.PCL.Theme

    ''' <summary>
    ''' 主题模块入口。依赖 pcl.core 提供的配置服务（没有也能跑，只是不记忆选择）。
    ''' </summary>
    Public Partial Class ThemeModule
        Implements IPclModule, IFrameworkSurface

        Private _log As ILogger
        Private _service As ThemeService

        Public Sub OnLoad(context As IModuleContext) Implements IPclModule.OnLoad
            _log = context.Log

            Dim settings As ISettingsService = Nothing
            If Not context.Services.TryGet(Of ISettingsService)(settings) Then
                _log.Warn("未找到配置服务，主题选择不会被记住")
            End If

            _service = New ThemeService(_log, settings)
            context.Services.Register(Of IThemeService)(_service)
            context.Data("theme") = _service
            _log.Info($"主题模块已装载，可用主题 {_service.Themes.Count} 套")
        End Sub

        Public Sub OnStart() Implements IPclModule.OnStart
            If _service Is Nothing Then Return
            Dim saved = _service.SavedThemeId
            If _service.Apply(saved) Then Return
            _log.Warn($"上次的主题 {saved} 已不存在，回退到默认主题")
            _service.Apply(ThemePresets.DefaultTheme.Id)
        End Sub

        Public Sub OnStop() Implements IPclModule.OnStop
            If _log IsNot Nothing Then _log.Info("主题模块已停止")
        End Sub

    End Class

End Namespace
