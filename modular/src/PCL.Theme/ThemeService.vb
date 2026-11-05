Imports System.Collections.Generic
Imports System.Windows
Imports System.Windows.Media
Imports PCL.Core
Imports PCL.Modular.Abstractions
Imports PCL.Modular.Contracts

Namespace Global.PCL.Theme

    ''' <summary>
    ''' 主题服务：把 ThemePalette 算出来的颜色写进 WPF 资源字典。
    '''
    ''' 与上游的区别：上游直接改 Application.Current.Resources 且强依赖 FrmMain.PanTitle；
    ''' 这里只做“算出颜色 + 写入资源 + 抛事件”，窗口自己的渐变由订阅方处理，
    ''' 这样主题模块不需要知道任何窗口类型。
    ''' </summary>
    Public Class ThemeService
        Implements IThemeService

        Private ReadOnly _log As ILogger
        Private ReadOnly _settings As ISettingsService
        Private ReadOnly _themes As List(Of ThemeInfo) = ThemePresets.CreateAll()
        Private _current As ThemeInfo
        Private _palette As ThemePalette

        Public Sub New(log As ILogger, settings As ISettingsService)
            _log = log
            _settings = settings
            _current = _themes(0)
            _palette = New ThemePalette(_current)
        End Sub

        Public ReadOnly Property Themes As IReadOnlyList(Of ThemeInfo) Implements IThemeService.Themes
            Get
                Return _themes
            End Get
        End Property

        Public ReadOnly Property Current As ThemeInfo Implements IThemeService.Current
            Get
                Return _current
            End Get
        End Property

        Public ReadOnly Property CurrentPalette As ThemePalette
            Get
                Return _palette
            End Get
        End Property

        Public Event ThemeChanged As Action(Of ThemeInfo) Implements IThemeService.ThemeChanged

        Public Function Apply(themeId As String) As Boolean Implements IThemeService.Apply
            Dim target = _themes.Find(Function(t) String.Equals(t.Id, themeId, StringComparison.OrdinalIgnoreCase))
            If target Is Nothing Then
                _log.Warn($"主题 {themeId} 不存在，保持当前主题 {_current.Id}")
                Return False
            End If

            _current = target
            _palette = New ThemePalette(target)

            Dim app = System.Windows.Application.Current
            If app Is Nothing Then
                ' 非 UI 进程（例如命令行验证）不算失败：颜色已经算好，窗口起来时再写
                _log.Info($"主题已切换为 {target.Name}（当前无 Application，颜色仅计算未写入资源）")
                RaiseEvent ThemeChanged(target)
                Return True
            End If

            If app.Dispatcher.CheckAccess() Then
                WriteResources(app)
            Else
                app.Dispatcher.Invoke(Sub() WriteResources(app))
            End If

            If _settings IsNot Nothing Then
                _settings.SetValue(Of String)("UiThemeId", target.Id)
            End If
            _log.Info($"主题已切换为 {target.Name}（色相 {target.Hue}，亮度偏移 {target.LightAdjust}）")
            RaiseEvent ThemeChanged(target)
            Return True
        End Function

        Private Sub WriteResources(application As Application)
            For Each pair In _palette.Tokens
                If pair.Key.StartsWith("ColorObject", StringComparison.Ordinal) Then
                    application.Resources(pair.Key) = CType(pair.Value, Color)
                Else
                    Dim brush = New SolidColorBrush(CType(pair.Value, Color))
                    brush.Freeze()
                    application.Resources(pair.Key) = brush
                End If
            Next
        End Sub

        Public Function GetColor(token As String) As String Implements IThemeService.GetColor
            Return _palette.GetHex(token)
        End Function

        ''' <summary>上次生效的主题 Id；没有配置服务或没存过时返回默认主题。</summary>
        Public ReadOnly Property SavedThemeId As String
            Get
                If _settings Is Nothing Then Return ThemePresets.DefaultTheme.Id
                Dim value = _settings.GetValue(Of String)("UiThemeId", ThemePresets.DefaultTheme.Id)
                Return If(String.IsNullOrWhiteSpace(value), ThemePresets.DefaultTheme.Id, value)
            End Get
        End Property

        ''' <summary>顶栏三段色标，交给宿主自己去画。</summary>
        Public Function GetTopbarGradient() As IReadOnlyList(Of ThemeGradientStop) Implements IThemeService.GetTopbarGradient
            Dim hues = _palette.GetTopbarHues()
            Dim lightness = New Double() {48 + _current.LightAdjust, 54 + _current.LightAdjust, 48 + _current.LightAdjust}
            Dim offsets = New Double() {0, 0.5, 1}
            Dim stops = New List(Of ThemeGradientStop)
            For index = 0 To 2
                stops.Add(New ThemeGradientStop With {
                    .Offset = offsets(index),
                    .Color = New MyColor().FromHSL2(hues(index), _current.Saturation, lightness(index)).ToHex()})
            Next
            Return stops
        End Function

        ''' <summary>顶栏渐变（上游 ThemeRefreshMain 里的同一算法）。</summary>
        Public Function CreateTopbarBrush() As LinearGradientBrush
            Dim hues = _palette.GetTopbarHues()
            Dim saturation = _current.Saturation
            Dim adjust = _current.LightAdjust
            Dim brush = New LinearGradientBrush With {.StartPoint = New Point(0, 0), .EndPoint = New Point(1, 0)}
            Dim offsets = New Double() {0, 0.5, 1}
            Dim lightness = New Double() {48 + adjust, 54 + adjust, 48 + adjust}
            For index = 0 To 2
                brush.GradientStops.Add(New GradientStop With {
                    .Offset = offsets(index),
                    .Color = CType(New MyColor().FromHSL2(hues(index), saturation, lightness(index)), Color)})
            Next
            brush.Freeze()
            Return brush
        End Function

    End Class

End Namespace
