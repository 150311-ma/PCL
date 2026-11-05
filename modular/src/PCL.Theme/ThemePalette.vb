Imports System.Collections.Generic
Imports PCL.Core
Imports PCL.Modular.Contracts

Namespace Global.PCL.Theme

    ''' <summary>
    ''' 由一组 HSL 参数算出整套颜色令牌。
    '''
    ''' 迁移自上游 ModSecret.ThemeRefresh：同样的分段亮度修正、同样的 8 级主色 +
    ''' 8 级灰阶 + 两个背景色，令牌名与上游 Application.xaml 完全一致，
    ''' 因此原控件里的 {DynamicResource ColorBrush3} 不需要改名。
    ''' </summary>
    Public Class ThemePalette

        Public Sub New(theme As ThemeInfo)
            Me.Theme = theme
            Rebuild()
        End Sub

        Public ReadOnly Property Theme As ThemeInfo

        ''' <summary>令牌名 → 颜色，顺序与上游一致。</summary>
        Public ReadOnly Property Tokens As New Dictionary(Of String, MyColor)(StringComparer.Ordinal)

        Public Sub Rebuild()
            Dim hue = Theme.Hue
            Dim saturation = Theme.Saturation
            Dim adjust = Theme.LightAdjust

            Dim color1 = New MyColor().FromHSL2(hue, saturation * 0.2, 25 + adjust * 0.3)
            Dim color2 = New MyColor().FromHSL2(hue, saturation, 45 + adjust)
            Dim color3 = New MyColor().FromHSL2(hue, saturation, 55 + adjust)
            Dim color4 = New MyColor().FromHSL2(hue, saturation, 65 + adjust)
            Dim color5 = New MyColor().FromHSL2(hue, saturation, 80 + adjust * 0.4)
            Dim color6 = New MyColor().FromHSL2(hue, saturation, 91 + adjust * 0.1)
            Dim color7 = New MyColor().FromHSL2(hue, saturation, 95)
            Dim color8 = New MyColor().FromHSL2(hue, saturation, 97)

            Dim gray1 = New MyColor(64, 64, 64)
            Dim gray2 = New MyColor(115, 115, 115)
            Dim gray3 = New MyColor(140, 140, 140)
            Dim gray4 = New MyColor(166, 166, 166)
            Dim gray5 = New MyColor(204, 204, 204)
            Dim gray6 = New MyColor(235, 235, 235)
            Dim gray7 = New MyColor(240, 240, 240)
            Dim gray8 = New MyColor(245, 245, 245)

            Dim background0 = color4 * 0.4 + color5 * 0.4 + gray4 * 0.2
            Dim background1 = New MyColor(190, color7)

            Tokens.Clear()
            Tokens("ColorBrush1") = color1
            Tokens("ColorBrush2") = color2
            Tokens("ColorBrush3") = color3
            Tokens("ColorBrush4") = color4
            Tokens("ColorBrush5") = color5
            Tokens("ColorBrush6") = color6
            Tokens("ColorBrush7") = color7
            Tokens("ColorBrush8") = color8
            Tokens("ColorBrushBg0") = background0
            Tokens("ColorBrushBg1") = background1
            Tokens("ColorObject1") = color1
            Tokens("ColorObject2") = color2
            Tokens("ColorObject3") = color3
            Tokens("ColorObject4") = color4
            Tokens("ColorObject5") = color5
            Tokens("ColorObject6") = color6
            Tokens("ColorObject7") = color7
            Tokens("ColorObject8") = color8
            Tokens("ColorObjectBg0") = background0
            Tokens("ColorObjectBg1") = background1
            Tokens("ColorBrushGray1") = gray1
            Tokens("ColorBrushGray2") = gray2
            Tokens("ColorBrushGray3") = gray3
            Tokens("ColorBrushGray4") = gray4
            Tokens("ColorBrushGray5") = gray5
            Tokens("ColorBrushGray6") = gray6
            Tokens("ColorBrushGray7") = gray7
            Tokens("ColorBrushGray8") = gray8
        End Sub

        ''' <summary>顶栏渐变的两端色相：0 表示纯色。</summary>
        Public Function GetTopbarHues() As Double()
            Dim delta = Theme.TopbarDelta
            Return New Double() {Theme.Hue - delta, Theme.Hue, Theme.Hue + delta}
        End Function

        Public Function GetHex(token As String) As String
            Dim color As MyColor = Nothing
            If Tokens.TryGetValue(token, color) Then Return color.ToHex()
            Return "#00000000"
        End Function

    End Class

End Namespace
