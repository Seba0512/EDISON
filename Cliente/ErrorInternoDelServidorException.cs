namespace Cliente;

internal sealed class ErrorInternoDelServidorException : Exception
{
    public ErrorInternoDelServidorException(string mensaje)
        : base(mensaje)
    {
    }
}
