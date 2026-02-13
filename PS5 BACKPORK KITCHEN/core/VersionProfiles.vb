Public Module VersionProfiles

    Public ReadOnly SdkPairs As New Dictionary(Of Integer, (PS5 As UInteger, PS4 As UInteger)) From {
        {1, (&H1000050UI, &H7590001UI)},
        {2, (&H2000009UI, &H8050001UI)},
        {3, (&H3000027UI, &H8540001UI)},
        {4, (&H4000031UI, &H9040001UI)},
        {5, (&H5000033UI, &H9590001UI)},
        {6, (&H6000038UI, &H10090001UI)},
        {7, (&H7000038UI, &H10590001UI)},
        {8, (&H8000041UI, &H11090001UI)},
        {9, (&H9000040UI, &H11590001UI)},
        {10, (&H10000040UI, &H12090001UI)}
    }

    'Public Function BuildPs5SdkList() As List(Of SdkComboItem)

    '    Dim list As New List(Of SdkComboItem)

    '    For Each kv In VersionProfiles.SdkPairs

    '        Dim ps5 = kv.Value.PS5
    '        Dim ps4 = kv.Value.PS4

    '        Dim fw = ToFirmware(ps5)

    '        list.Add(New SdkComboItem With {
    '            .Key = kv.Key,
    '            .Display = $"{fw}",
    '            .Ps5Sdk = ps5,
    '            .Ps4Sdk = ps4
    '        })
    '    Next

    '    list.Sort(Function(a, b) a.Ps5Sdk.CompareTo(b.Ps5Sdk))

    '    Return list
    'End Function
    '    Public Function BuildPs5SdkList() As List(Of SdkComboItem)

    '        Dim list As New List(Of SdkComboItem)

    '        For Each kv In SdkPairs

    '            Dim ps5 = kv.Value.PS5
    '            Dim ps4 = kv.Value.PS4

    '            Dim fw = ToFirmware(ps5)

    '            list.Add(New SdkComboItem With {
    '            .Key = kv.Key,
    '            .Display = fw.ToString(
    '                Globalization.CultureInfo.InvariantCulture),
    '            .Ps5Sdk = ps5,
    '            .Ps4Sdk = ps4
    '        })
    '        Next

    '        list.Sort(Function(a, b) a.Ps5Sdk.CompareTo(b.Ps5Sdk))

    '        Return list
    '    End Function

    'End Module
    Public Function BuildPs5SdkList() As List(Of SdkComboItem)

        Dim list As New List(Of SdkComboItem)

        For Each kv In SdkPairs

            Dim ps5 = kv.Value.PS5
            Dim ps4 = kv.Value.PS4

            list.Add(New SdkComboItem With {
                .Key = kv.Key,
                .Display = kv.Key.ToString(),
                .Ps5Sdk = ps5,
                .Ps4Sdk = ps4
            })

        Next

        list.Sort(Function(a, b) a.Key.CompareTo(b.Key))

        Return list
    End Function

End Module

Public Class SdkComboItem
    Public Property Key As Integer
    Public Property Display As String
    Public Property Ps5Sdk As UInteger
    Public Property Ps4Sdk As UInteger
End Class