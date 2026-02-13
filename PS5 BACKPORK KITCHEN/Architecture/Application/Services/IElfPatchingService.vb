Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Models
Imports PS5_BACKPORK_KITCHEN.Architecture.Domain.Results

Namespace Architecture.Application.Services
    ''' <summary>
    ''' Service interface for ELF patching operations
    ''' </summary>
    Public Interface IElfPatchingService
        ''' <summary>
        ''' Patches a single ELF file to target SDK version
        ''' </summary>
        Function PatchFileAsync(filePath As String,
                               targetSdk As Long,
                               cancellationToken As Threading.CancellationToken) _
                               As Task(Of Result(Of PatchResult))

        ''' <summary>
        ''' Checks if a file can be patched
        ''' </summary>
        Function CanPatchFileAsync(filePath As String) As Task(Of Result(Of Boolean))

        ''' <summary>
        ''' Detects the current SDK version of a file
        ''' </summary>
        Function DetectSdkVersionAsync(filePath As String) As Task(Of Result(Of Long))
    End Interface
End Namespace
