Imports System.IO

Namespace Global.PCL.Core

    ''' <summary>
    ''' 路径约定。PCL 原来把路径散在 ModBase 里（PathExe/PathTemp/PathImage），
    ''' 这里集中成一处，其它模块通过服务拿到，不再各自拼字符串。
    ''' </summary>
    Public Class PclPaths

        Public Sub New(baseDirectory As String, dataDirectory As String)
            Me.BaseDirectory = EnsureSlash(baseDirectory)
            Me.DataDirectory = EnsureSlash(dataDirectory)
        End Sub

        ''' <summary>程序所在目录，以 \ 结尾。</summary>
        Public ReadOnly Property BaseDirectory As String

        ''' <summary>用户数据目录（配置、缓存、日志），以 \ 结尾。</summary>
        Public ReadOnly Property DataDirectory As String

        Public ReadOnly Property SettingsFile As String
            Get
                Return DataDirectory & "settings.json"
            End Get
        End Property

        Public ReadOnly Property LogsDirectory As String
            Get
                Return EnsureSlash(DataDirectory & "logs")
            End Get
        End Property

        Public ReadOnly Property CacheDirectory As String
            Get
                Return EnsureSlash(DataDirectory & "cache")
            End Get
        End Property

        ''' <summary>确保目录存在；失败时返回 False 而不抛，调用方决定是否降级。</summary>
        Public Function EnsureDirectories() As Boolean
            Try
                Directory.CreateDirectory(DataDirectory)
                Directory.CreateDirectory(LogsDirectory)
                Directory.CreateDirectory(CacheDirectory)
                Return True
            Catch
                Return False
            End Try
        End Function

        Private Shared Function EnsureSlash(path As String) As String
            If String.IsNullOrEmpty(path) Then Return path
            Return If(path.EndsWith("\") OrElse path.EndsWith("/"), path, path & "\")
        End Function

    End Class

End Namespace
