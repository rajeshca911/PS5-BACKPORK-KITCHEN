Imports System.Text

Public Class UfsCommandBuilder

    Public Shared Function Build(opts As UfsBuildOptions,
                                 exePath As String) As String

        Dim sb As New StringBuilder

        ' exe
        sb.Append(Quote(exePath))

        ' command — adjust later if needed
        sb.Append(" makefs")

        ' source + output
        sb.Append(" -i ").Append(Quote(opts.SourceFolder))
        sb.Append(" -o ").Append(Quote(opts.OutputFile))

        ' preset vs custom
        If opts.UsePs5Preset Then

            ' safe defaults for PS5
            sb.Append(" --ps5")

        Else

            If Not String.IsNullOrWhiteSpace(opts.VolumeLabel) Then
                sb.Append(" --label ").Append(Quote(opts.VolumeLabel))
            End If

            If Not String.IsNullOrWhiteSpace(opts.BlockSize) AndAlso
               opts.BlockSize <> "Auto" Then
                sb.Append(" --block ").Append(opts.BlockSize)
            End If

            If Not String.IsNullOrWhiteSpace(opts.OptimizeMode) Then
                sb.Append(" --opt ").Append(opts.OptimizeMode.ToLower())
            End If

            If Not String.IsNullOrWhiteSpace(opts.ExtraFlags) Then
                sb.Append(" ").Append(opts.ExtraFlags)
            End If

        End If

        Return sb.ToString()

    End Function


    Private Shared Function Quote(s As String) As String
        Return """" & s & """"
    End Function

End Class

