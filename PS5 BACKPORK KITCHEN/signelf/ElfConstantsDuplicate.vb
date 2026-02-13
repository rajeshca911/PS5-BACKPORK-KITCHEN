Module ElfConstantsDuplicate
    'Public ReadOnly ELF_MAGIC As Byte() = {&H7F, &H45, &H4C, &H46}

    Public Const ELFCLASS64 As Byte = &H2
    Public Const ELFDATA2LSB As Byte = &H1

    Public Const EM_X86_64 As UShort = &H3E
    Public Const EV_CURRENT As UInteger = &H1

    Public Const ET_EXEC As UShort = &H2
    Public Const ET_SCE_EXEC As UShort = &HFE00
    Public Const ET_SCE_EXEC_ASLR As UShort = &HFE10
    Public Const ET_SCE_DYNAMIC As UShort = &HFE18

End Module