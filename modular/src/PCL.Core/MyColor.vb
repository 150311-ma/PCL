' 从 PCL/Modules/Base/ModBase.vb 的 MyColor 迁移而来。
' 迁移原则：只保留颜色运算本身，去掉对 PCL 全局状态（Settings/FrmMain）的依赖，
' 使它可以被任何模块直接引用。
Imports System.Windows.Media

Namespace Global.PCL.Core

    ''' <summary>
    ''' PCL 的颜色运算类型：以 Double 分量保存 ARGB，支持 HSL 换算与颜色插值。
    ''' HSL 换算里带有 PCL 特有的“亮度重心”修正（FromHSL2），这是主题色看起来不发灰的原因。
    ''' </summary>
    Public Class MyColor

        Public A As Double = 255
        Public R As Double = 0
        Public G As Double = 0
        Public B As Double = 0

#Region "类型转换"

        Public Shared Widening Operator CType(text As String) As MyColor
            Return New MyColor(text)
        End Operator

        Public Shared Widening Operator CType(color As Color) As MyColor
            Return New MyColor(color)
        End Operator

        Public Shared Widening Operator CType(color As MyColor) As Color
            Return Global.System.Windows.Media.Color.FromArgb(ClampToByte(color.A), ClampToByte(color.R), ClampToByte(color.G), ClampToByte(color.B))
        End Operator

        Public Shared Widening Operator CType(brush As SolidColorBrush) As MyColor
            Return New MyColor(brush.Color)
        End Operator

        Public Shared Widening Operator CType(color As MyColor) As SolidColorBrush
            Dim brush = New SolidColorBrush(Global.System.Windows.Media.Color.FromArgb(ClampToByte(color.A), ClampToByte(color.R), ClampToByte(color.G), ClampToByte(color.B)))
            brush.Freeze()
            Return brush
        End Operator

#End Region

#Region "颜色运算"

        Public Shared Operator +(a As MyColor, b As MyColor) As MyColor
            Return New MyColor With {.A = a.A + b.A, .R = a.R + b.R, .G = a.G + b.G, .B = a.B + b.B}
        End Operator

        Public Shared Operator -(a As MyColor, b As MyColor) As MyColor
            Return New MyColor With {.A = a.A - b.A, .R = a.R - b.R, .G = a.G - b.G, .B = a.B - b.B}
        End Operator

        Public Shared Operator *(a As MyColor, factor As Double) As MyColor
            Return New MyColor With {.A = a.A * factor, .R = a.R * factor, .G = a.G * factor, .B = a.B * factor}
        End Operator

        Public Shared Operator /(a As MyColor, factor As Double) As MyColor
            Return New MyColor With {.A = a.A / factor, .R = a.R / factor, .G = a.G / factor, .B = a.B / factor}
        End Operator

        Public Shared Operator =(a As MyColor, b As MyColor) As Boolean
            If a Is Nothing AndAlso b Is Nothing Then Return True
            If a Is Nothing OrElse b Is Nothing Then Return False
            Return a.A = b.A AndAlso a.R = b.R AndAlso a.G = b.G AndAlso a.B = b.B
        End Operator

        Public Shared Operator <>(a As MyColor, b As MyColor) As Boolean
            Return Not (a = b)
        End Operator

        Public Shared Function Lerp(valueA As MyColor, valueB As MyColor, percent As Double) As MyColor
            Return Round(valueA * (1 - percent) + valueB * percent, 6)
        End Function

        Public Shared Function Round(color As MyColor, Optional digits As Integer = 0) As MyColor
            Return New MyColor With {
                .A = Math.Round(color.A, digits),
                .R = Math.Round(color.R, digits),
                .G = Math.Round(color.G, digits),
                .B = Math.Round(color.B, digits)}
        End Function

        ''' <summary>把任意数值夹到 0~255 的 Byte。</summary>
        Public Shared Function ClampToByte(value As Double) As Byte
            If value < 0 Then value = 0
            If value > 255 Then value = 255
            Return CByte(Math.Round(value))
        End Function

#End Region

#Region "构造函数"

        Public Sub New()
        End Sub

        Public Sub New(color As Color)
            A = color.A
            R = color.R
            G = color.G
            B = color.B
        End Sub

        Public Sub New(hexText As String)
            Dim parsed As Color = ColorConverter.ConvertFromString(hexText)
            A = parsed.A
            R = parsed.R
            G = parsed.G
            B = parsed.B
        End Sub

        Public Sub New(alpha As Double, color As MyColor)
            A = alpha
            R = color.R
            G = color.G
            B = color.B
        End Sub

        Public Sub New(red As Double, green As Double, blue As Double)
            A = 255
            R = red
            G = green
            B = blue
        End Sub

        Public Sub New(alpha As Double, red As Double, green As Double, blue As Double)
            A = alpha
            R = red
            G = green
            B = blue
        End Sub

#End Region

#Region "HSL"

        Private Function HueChannel(v1 As Double, v2 As Double, hueValue As Double) As Double
            If hueValue < 0 Then hueValue += 1
            If hueValue > 1 Then hueValue -= 1
            If hueValue < 0.16667 Then Return v1 + (v2 - v1) * 6 * hueValue
            If hueValue < 0.5 Then Return v2
            If hueValue < 0.66667 Then Return v1 + (v2 - v1) * (4 - hueValue * 6)
            Return v1
        End Function

        ''' <summary>标准 HSL 换算。</summary>
        Public Function FromHSL(hue As Double, saturation As Double, lightness As Double) As MyColor
            A = 255
            If saturation = 0 Then
                R = lightness * 2.55
                G = R
                B = R
                Return Me
            End If

            Dim h = hue / 360
            Dim s = saturation / 100
            Dim l = lightness / 100
            s = If(l < 0.5, s * l + l, s * (1.0 - l) + l)
            l = 2 * l - s
            R = 255 * HueChannel(l, s, h + 1 / 3)
            G = 255 * HueChannel(l, s, h)
            B = 255 * HueChannel(l, s, h - 1 / 3)
            Return Me
        End Function

        ''' <summary>
        ''' PCL 的 HSL 换算。在标准 HSL 之上按色相分段修正亮度重心，
        ''' 让黄、绿等本身偏亮的色相在同样的“亮度参数”下看起来一致。
        ''' </summary>
        Public Function FromHSL2(hue As Double, saturation As Double, lightness As Double) As MyColor
            A = 255
            If saturation = 0 Then
                R = lightness * 2.55
                G = R
                B = R
                Return Me
            End If

            hue = (hue + 3600000) Mod 360
            Dim centers As Double() = {
                0.1, -0.06, -0.3,
                -0.19, -0.15, -0.24,
                -0.32, -0.09, 0.18,
                0.05, -0.12, -0.02,
                0.1, -0.06}

            Dim center = hue / 30.0
            Dim segment = CInt(Math.Floor(center))
            center = 50 - ((1 - center + segment) * centers(segment) + (center - segment) * centers(segment + 1)) * saturation
            lightness = If(lightness < center, lightness / center, 1 + (lightness - center) / (100 - center)) * 50
            Return FromHSL(hue, saturation, lightness)
        End Function

#End Region

        Public Function ToHex() As String
            Return $"#{ClampToByte(R):X2}{ClampToByte(G):X2}{ClampToByte(B):X2}"
        End Function

        Public Overrides Function ToString() As String
            Return $"({A},{R},{G},{B})"
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me = TryCast(obj, MyColor)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return A.GetHashCode() Xor R.GetHashCode() Xor G.GetHashCode() Xor B.GetHashCode()
        End Function

    End Class

End Namespace
