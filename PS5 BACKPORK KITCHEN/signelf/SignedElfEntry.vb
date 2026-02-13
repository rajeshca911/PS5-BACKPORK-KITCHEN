Imports System.IO

Public Class SignedElfEntry

    ' ---- STRUCT <4Q> ----
    Public Props As ULong

    Public Offset As ULong
    Public FileSize As ULong
    Public MemSize As ULong

    Public ReadOnly Index As Integer
    Public Data As Byte()

    Public Sub New(idx As Integer)
        Index = idx
        Props = 0

    End Sub

    Public Sub Save(bw As BinaryWriter)
        bw.Write(Props)
        bw.Write(Offset)
        bw.Write(FileSize)
        bw.Write(MemSize)
    End Sub

    ' ---- Bitfield layout ----
    Public Const PROPS_ORDER_SHIFT As Integer = 0

    Public Const PROPS_ORDER_MASK As ULong = &H1UL

    Public Const PROPS_ENCRYPTED_SHIFT As Integer = 1
    Public Const PROPS_ENCRYPTED_MASK As ULong = &H1UL

    Public Const PROPS_SIGNED_SHIFT As Integer = 2
    Public Const PROPS_SIGNED_MASK As ULong = &H1UL

    Public Const PROPS_COMPRESSED_SHIFT As Integer = 3
    Public Const PROPS_COMPRESSED_MASK As ULong = &H1UL

    Public Const PROPS_WINDOW_BITS_SHIFT As Integer = 8
    Public Const PROPS_WINDOW_BITS_MASK As ULong = &H7UL

    Public Const PROPS_HAS_BLOCKS_SHIFT As Integer = 11
    Public Const PROPS_HAS_BLOCKS_MASK As ULong = &H1UL

    Public Const PROPS_BLOCK_SIZE_SHIFT As Integer = 12
    Public Const PROPS_BLOCK_SIZE_MASK As ULong = &HFUL

    Public Const PROPS_HAS_DIGESTS_SHIFT As Integer = 16
    Public Const PROPS_HAS_DIGESTS_MASK As ULong = &H1UL

    Public Const PROPS_HAS_EXTENTS_SHIFT As Integer = 17
    Public Const PROPS_HAS_EXTENTS_MASK As ULong = &H1UL

    'Public Const PROPS_HAS_META_SEGMENT_SHIFT As Integer = 20
    'Public Const PROPS_HAS_META_SEGMENT_MASK As ULong = &H1UL

    Public Const PROPS_SEGMENT_INDEX_SHIFT As Integer = 20
    Public Const PROPS_SEGMENT_INDEX_MASK As ULong = &HFFFFUL

    Public Const DEFAULT_BLOCK_SIZE As Integer = &H1000

    ' ---- Order ----
    Public Property Order As Integer
        Get
            Return CInt((Props >> PROPS_ORDER_SHIFT) And PROPS_ORDER_MASK)
        End Get
        Set(value As Integer)
            Props = Props And Not (PROPS_ORDER_MASK << PROPS_ORDER_SHIFT)
            Props = Props Or (CULng(value) And PROPS_ORDER_MASK) << PROPS_ORDER_SHIFT
        End Set
    End Property

    ' ---- Encrypted ----
    Public Property Encrypted As Boolean
        Get
            Return ((Props >> PROPS_ENCRYPTED_SHIFT) And PROPS_ENCRYPTED_MASK) <> 0
        End Get
        Set(value As Boolean)
            Props = Props And Not (PROPS_ENCRYPTED_MASK << PROPS_ENCRYPTED_SHIFT)
            If value Then Props = Props Or (PROPS_ENCRYPTED_MASK << PROPS_ENCRYPTED_SHIFT)
        End Set
    End Property

    ' ---- Signed ----
    Public Property Signed As Boolean
        Get
            Return ((Props >> PROPS_SIGNED_SHIFT) And PROPS_SIGNED_MASK) <> 0
        End Get
        Set(value As Boolean)
            Props = Props And Not (PROPS_SIGNED_MASK << PROPS_SIGNED_SHIFT)
            If value Then Props = Props Or (PROPS_SIGNED_MASK << PROPS_SIGNED_SHIFT)
        End Set
    End Property

    ' ---- Has blocks ----
    Public Property HasBlocks As Boolean
        Get
            Return ((Props >> PROPS_HAS_BLOCKS_SHIFT) And PROPS_HAS_BLOCKS_MASK) <> 0
        End Get
        Set(value As Boolean)
            Props = Props And Not (PROPS_HAS_BLOCKS_MASK << PROPS_HAS_BLOCKS_SHIFT)
            If value Then Props = Props Or (PROPS_HAS_BLOCKS_MASK << PROPS_HAS_BLOCKS_SHIFT)
        End Set
    End Property

    ' ---- Has digests ----
    Public Property HasDigests As Boolean
        Get
            Return ((Props >> PROPS_HAS_DIGESTS_SHIFT) And PROPS_HAS_DIGESTS_MASK) <> 0
        End Get
        Set(value As Boolean)
            Props = Props And Not (PROPS_HAS_DIGESTS_MASK << PROPS_HAS_DIGESTS_SHIFT)
            If value Then Props = Props Or (PROPS_HAS_DIGESTS_MASK << PROPS_HAS_DIGESTS_SHIFT)
        End Set
    End Property

    ' ---- Block size ----
    Public Property BlockSize As Integer
        Get
            If HasBlocks Then
                Dim v = (Props >> PROPS_BLOCK_SIZE_SHIFT) And PROPS_BLOCK_SIZE_MASK
                Return 1 << (12 + CInt(v))
            Else
                Return DEFAULT_BLOCK_SIZE
            End If
        End Get
        Set(value As Integer)
            Props = Props And Not (PROPS_BLOCK_SIZE_MASK << PROPS_BLOCK_SIZE_SHIFT)
            If HasBlocks Then
                Dim v = BinaryHelpers.ILog2(value) - 12
                Props = Props Or (CULng(v) And PROPS_BLOCK_SIZE_MASK) << PROPS_BLOCK_SIZE_SHIFT
            End If
        End Set
    End Property

    ' ---- Segment index ----
    Public Property SegmentIndex As Integer
        Get
            Return CInt((Props >> PROPS_SEGMENT_INDEX_SHIFT) And PROPS_SEGMENT_INDEX_MASK)
        End Get
        Set(value As Integer)
            Props = Props And Not (PROPS_SEGMENT_INDEX_MASK << PROPS_SEGMENT_INDEX_SHIFT)
            Props = Props Or (CULng(value) And PROPS_SEGMENT_INDEX_MASK) << PROPS_SEGMENT_INDEX_SHIFT
        End Set
    End Property

End Class