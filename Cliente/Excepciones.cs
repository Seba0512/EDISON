namespace Cliente;

public sealed class OperacionRechazadaException : Exception
{
    public OperacionRechazadaException(string mensaje) : base(mensaje)
    {
    }
}

// Un IN no tiene arreglo. La excepción llega hasta Main y el cliente cierra la conexión enseguida.
public sealed class ErrorInternoDelServidorException : Exception
{
    public ErrorInternoDelServidorException(string mensaje) : base(mensaje)
    {
    }
}
