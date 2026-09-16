namespace Cliente.Excepciones;

public sealed class ErrorInternoDelServidorException : Exception
{
    public ErrorInternoDelServidorException(string mensaje) : base(mensaje)
    {
    }
}
