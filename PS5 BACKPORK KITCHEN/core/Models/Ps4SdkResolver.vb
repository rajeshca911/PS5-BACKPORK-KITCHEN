Public Module Ps4SdkResolver

    Public Function GetDefaultPs4Sdk(ps5Sdk As UInteger) As UInteger

        Select Case ps5Sdk
            Case &H5000033UI : Return &H9590001UI
            Case &H6000038UI : Return &H10090001UI
            Case &H7000038UI : Return &H10590001UI
            Case &H8000041UI : Return &H11090001UI
            Case &H9000040UI : Return &H11590001UI
            Case &H10000040UI : Return &H12090001UI
            Case Else
                ' Safe fallback
                Return &H9590001UI
        End Select

    End Function

End Module