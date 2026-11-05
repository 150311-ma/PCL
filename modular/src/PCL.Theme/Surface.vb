Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Media
Imports PCL.Modular.Abstractions
Imports PCL.Modular.Contracts

Namespace Global.PCL.Theme

    ''' <summary>主题框架的界面：把主题摊开，点一下换一套。</summary>
    Partial Public Class ThemeModule

        Public ReadOnly Property Title As String Implements IFrameworkSurface.Title
            Get
                Return "主题"
            End Get
        End Property

        Public Function CreateView(context As IModuleContext) As Object Implements IFrameworkSurface.CreateView
            Dim root As New StackPanel()
            Dim service As IThemeService = Nothing
            context.Services.TryGet(Of IThemeService)(service)
            Dim theme = TryCast(service, ThemeService)

            If theme Is Nothing Then
                root.Children.Add(Note("主题服务没拿到，这个界面没法用。"))
                Return root
            End If

            Dim status As New TextBlock With {
                .Text = "当前：" & theme.Current.Name,
                .FontSize = 12,
                .Margin = New Thickness(0, 0, 0, 16),
                .Foreground = New SolidColorBrush(Color.FromRgb(&H7C, &H87, &H98))
            }
            root.Children.Add(status)

            Dim grid As New UniformGrid With {.Columns = 4}
            For Each item In theme.Themes
                Dim card As New Button With {
                    .Content = item.Name,
                    .Tag = item.Id,
                    .Height = 54,
                    .Margin = New Thickness(0, 0, 10, 10),
                    .Cursor = System.Windows.Input.Cursors.Hand,
                    .Background = New SolidColorBrush(Color.FromRgb(&HF5, &HF8, &HFD)),
                    .BorderBrush = New SolidColorBrush(Color.FromRgb(&HE3, &HEC, &HF7)),
                    .BorderThickness = New Thickness(1)
                }
                AddHandler card.Click,
                    Sub(sender As Object, e As RoutedEventArgs)
                        Dim id = CStr(CType(sender, Button).Tag)
                        If Not theme.Apply(id) Then Return
                        status.Text = "当前：" & theme.Current.Name
                        context.Log.Info("界面里切换主题：" & id)
                    End Sub
                grid.Children.Add(card)
            Next

            root.Children.Add(grid)
            root.Children.Add(Note("点一下就换。改完会记住，下次打开还是这套。"))
            Return root
        End Function

        Private Shared Function Note(text As String) As FrameworkElement
            Return New TextBlock With {
                .Text = text,
                .FontSize = 12,
                .TextWrapping = TextWrapping.Wrap,
                .LineHeight = 20,
                .Margin = New Thickness(0, 14, 0, 0),
                .Foreground = New SolidColorBrush(Color.FromRgb(&H4A, &H55, &H63))
            }
        End Function

    End Class

End Namespace
