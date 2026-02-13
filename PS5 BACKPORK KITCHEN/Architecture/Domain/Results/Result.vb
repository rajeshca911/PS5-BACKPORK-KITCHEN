Namespace Architecture.Domain.Results
    ''' <summary>
    ''' Represents the result of an operation that can either succeed with a value or fail with an error.
    ''' Enables functional error handling without exceptions.
    ''' </summary>
    Public Class Result(Of T)
        Public ReadOnly Property IsSuccess As Boolean
        Public ReadOnly Property Value As T
        Public ReadOnly Property [Error] As Errors.DomainError

        Private Sub New(value As T, [error] As Errors.DomainError, isSuccess As Boolean)
            Me.Value = value
            Me.[Error] = [error]
            Me.IsSuccess = isSuccess
        End Sub

        ''' <summary>
        ''' Creates a successful result with a value
        ''' </summary>
        Public Shared Function Success(value As T) As Result(Of T)
            Return New Result(Of T)(value, Nothing, True)
        End Function

        ''' <summary>
        ''' Creates a failed result with an error
        ''' </summary>
        Public Shared Function Fail([error] As Errors.DomainError) As Result(Of T)
            Return New Result(Of T)(Nothing, [error], False)
        End Function

        ''' <summary>
        ''' Maps the value if success, otherwise propagates the error
        ''' </summary>
        Public Function Map(Of U)(mapper As Func(Of T, U)) As Result(Of U)
            If IsSuccess Then
                Return Result(Of U).Success(mapper(Value))
            Else
                Return Result(Of U).Fail([Error])
            End If
        End Function
    End Class

    ''' <summary>
    ''' Unit type for operations that don't return a meaningful value (like void)
    ''' </summary>
    Public Class Unit
        Public Shared ReadOnly Property Value As Unit = New Unit()
        Private Sub New()
        End Sub
    End Class
End Namespace
