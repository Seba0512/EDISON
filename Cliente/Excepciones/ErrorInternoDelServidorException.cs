namespace Cliente.Excepciones;

/// <summary>El servidor respondió IN (CR1): error irrecuperable, el proceso del cliente termina.</summary>
public sealed class ErrorInternoDelServidorException : Exception
{
    public ErrorInternoDelServidorException(string mensaje) : base(mensaje)
    {
    }
}
