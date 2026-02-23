Imports System.Drawing
Imports System.Windows.Forms

Public Class ExistingLoggerAdapter
    Implements ISelfLogger   ' or whatever your custom interface is called

    Private ReadOnly _rtb As RichTextBox

    Public Sub New(rtb As RichTextBox)
        _rtb = rtb
    End Sub

    Public Sub Debug(message As String) Implements ISelfLogger.Debug
        Logger.Log(_rtb, message, Color.Gray)
    End Sub

    Public Sub Info(message As String) Implements ISelfLogger.Info
        Logger.Log(_rtb, message, Color.Black)
    End Sub

    Public Sub Warning(message As String) Implements ISelfLogger.Warning
        Logger.Log(_rtb, message, Color.DarkOrange)
    End Sub

    Public Sub [Error](message As String) Implements ISelfLogger.Error
        Logger.Log(_rtb, message, Color.Red)
    End Sub

End Class