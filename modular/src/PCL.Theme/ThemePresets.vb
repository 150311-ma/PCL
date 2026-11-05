Imports PCL.Modular.Contracts

Namespace Global.PCL.Theme

    ''' <summary>
    ''' 主题预设。参数与上游 ModSecret 的 ColorHue / ColorSat / ColorLightAdjust / ColorHueTopbarDelta 一一对应，
    ''' 也就是说这里换主题等价于上游调用 ThemeRefresh()，颜色算法保持同源。
    ''' </summary>
    Public NotInheritable Class ThemePresets

        Private Sub New()
        End Sub

        Public Shared Function CreateAll() As List(Of ThemeInfo)
            Return New List(Of ThemeInfo) From {
                New ThemeInfo With {.Id = "pcl-blue", .Name = "PCL 蓝", .Hue = 210, .Saturation = 85, .LightAdjust = 0, .TopbarDelta = 0},
                New ThemeInfo With {.Id = "sky", .Name = "晴空", .Hue = 200, .Saturation = 80, .LightAdjust = 4, .TopbarDelta = 6},
                New ThemeInfo With {.Id = "deep-sea", .Name = "深海", .Hue = 220, .Saturation = 72, .LightAdjust = -12, .TopbarDelta = -8},
                New ThemeInfo With {.Id = "violet", .Name = "紫罗兰", .Hue = 268, .Saturation = 62, .LightAdjust = 0, .TopbarDelta = 8},
                New ThemeInfo With {.Id = "rose", .Name = "玫瑰", .Hue = 340, .Saturation = 68, .LightAdjust = 0, .TopbarDelta = 6},
                New ThemeInfo With {.Id = "sakura", .Name = "樱粉", .Hue = 330, .Saturation = 52, .LightAdjust = 8, .TopbarDelta = 10},
                New ThemeInfo With {.Id = "amber", .Name = "琥珀", .Hue = 35, .Saturation = 78, .LightAdjust = -4, .TopbarDelta = 7},
                New ThemeInfo With {.Id = "lemon", .Name = "柠檬", .Hue = 52, .Saturation = 76, .LightAdjust = -8, .TopbarDelta = 9},
                New ThemeInfo With {.Id = "lime", .Name = "青柠", .Hue = 100, .Saturation = 58, .LightAdjust = -6, .TopbarDelta = 8},
                New ThemeInfo With {.Id = "mint", .Name = "薄荷", .Hue = 162, .Saturation = 55, .LightAdjust = 0, .TopbarDelta = 6},
                New ThemeInfo With {.Id = "graphite", .Name = "水墨", .Hue = 210, .Saturation = 8, .LightAdjust = -6, .TopbarDelta = 0},
                New ThemeInfo With {.Id = "jump-red", .Name = "跳票红", .Hue = 356, .Saturation = 82, .LightAdjust = -8, .TopbarDelta = 5}
            }
        End Function

        Public Shared ReadOnly Property DefaultTheme As ThemeInfo
            Get
                Return CreateAll()(0)
            End Get
        End Property

    End Class

End Namespace
