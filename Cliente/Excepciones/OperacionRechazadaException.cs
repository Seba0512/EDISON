namespace Cliente.Excepciones;

public sealed class OperacionRechazadaException : Exception
{
    public OperacionRechazadaException(string mensaje) : base(mensaje)
    {
    }
}
