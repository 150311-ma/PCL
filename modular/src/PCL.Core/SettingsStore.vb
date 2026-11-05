Imports System.Collections.Generic
Imports System.IO
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports PCL.Modular.Contracts

Namespace Global.PCL.Core

    ''' <summary>
    ''' JSON 配置存储，实现 ISettingsService。
    '''
    ''' 与 PCL 原来的 Settings 类的区别：这里不做“选项声明”，只做键值持久化。
    ''' 选项的默认值由使用方给出，避免配置层反过来依赖 UI 层的选项定义。
    ''' </summary>
    Public Class SettingsStore
        Implements ISettingsService

        Private ReadOnly _filePath As String
        Private ReadOnly _syncRoot As New Object()
        Private _data As JObject = New JObject()
        Private _dirty As Boolean
        Private _lastError As String

        Public Sub New(filePath As String)
            _filePath = filePath
            Load()
        End Sub

        Public ReadOnly Property LastError As String Implements ISettingsService.LastError
            Get
                Return _lastError
            End Get
        End Property

        Public ReadOnly Property Keys As IReadOnlyCollection(Of String) Implements ISettingsService.Keys
            Get
                SyncLock _syncRoot
                    Return _data.Properties().Select(Function(p) p.Name).ToList()
                End SyncLock
            End Get
        End Property

        Public Function Load() As Boolean
            SyncLock _syncRoot
                Try
                    If Not File.Exists(_filePath) Then
                        _data = New JObject()
                        Return True
                    End If
                    Dim text = File.ReadAllText(_filePath)
                    _data = If(String.IsNullOrWhiteSpace(text), New JObject(), JObject.Parse(text))
                    _dirty = False
                    Return True
                Catch ex As Exception
                    ' 配置文件坏了不能让程序起不来：留档后从空配置开始
                    _data = New JObject()
                    _lastError = ex.Message
                    Return False
                End Try
            End SyncLock
        End Function

        Public Function GetValue(Of T)(key As String, Optional defaultValue As T = Nothing) As T Implements ISettingsService.GetValue
            If String.IsNullOrEmpty(key) Then Return defaultValue
            SyncLock _syncRoot
                Dim token = _data(key)
                If token Is Nothing OrElse token.Type = JTokenType.Null Then Return defaultValue
                Try
                    Return token.ToObject(Of T)()
                Catch ex As Exception
                    _lastError = $"读取 {key} 失败：{ex.Message}"
                    Return defaultValue
                End Try
            End SyncLock
        End Function

        Public Sub SetValue(Of T)(key As String, value As T) Implements ISettingsService.SetValue
            If String.IsNullOrEmpty(key) Then Return
            SyncLock _syncRoot
                _data(key) = If(value Is Nothing, JValue.CreateNull(), JToken.FromObject(value))
                _dirty = True
            End SyncLock
        End Sub

        Public Function Remove(key As String) As Boolean Implements ISettingsService.Remove
            SyncLock _syncRoot
                If _data.Property(key) Is Nothing Then Return False
                _data.Remove(key)
                _dirty = True
                Return True
            End SyncLock
        End Function

        Public Function Save() As Boolean Implements ISettingsService.Save
            SyncLock _syncRoot
                Try
                    Dim folder = Path.GetDirectoryName(_filePath)
                    If Not String.IsNullOrEmpty(folder) Then Directory.CreateDirectory(folder)
                    File.WriteAllText(_filePath, _data.ToString(Formatting.Indented))
                    _dirty = False
                    Return True
                Catch ex As Exception
                    _lastError = ex.Message
                    Return False
                End Try
            End SyncLock
        End Function

        Public ReadOnly Property HasUnsavedChanges As Boolean
            Get
                SyncLock _syncRoot
                    Return _dirty
                End SyncLock
            End Get
        End Property

    End Class

End Namespace
