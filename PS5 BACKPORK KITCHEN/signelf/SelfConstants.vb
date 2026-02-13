Public Module SelfConstants

    Public ReadOnly SELF_MAGIC As Byte() = {&H4F, &H15, &H3D, &H1D}

    Public Const SELF_VERSION As Byte = &H0
    Public Const SELF_MODE As Byte = &H1
    Public Const SELF_ENDIAN As Byte = &H1
    Public Const SELF_ATTRIBS As Byte = &H12

    Public Const SELF_KEY_TYPE As UInteger = &H101

    Public Const DIGEST_SIZE As Integer = &H20
    Public Const SIGNATURE_SIZE As Integer = &H100

    Public Const BLOCK_SIZE As Integer = &H4000
    Public Const DEFAULT_BLOCK_SIZE As Integer = &H1000

    Public ReadOnly EMPTY_DIGEST As Byte() = New Byte(DIGEST_SIZE - 1) {}
    Public ReadOnly EMPTY_SIGNATURE As Byte() = New Byte(SIGNATURE_SIZE - 1) {}

    ' Flags
    Public Const FLAGS_SEGMENT_SIGNED_SHIFT As Integer = 4

    Public Const FLAGS_SEGMENT_SIGNED_MASK As Integer = &H7

End Module