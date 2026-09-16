namespace Common.Excepciones;

public sealed class TransferenciaDeArchivoException : Exception
{
    public TransferenciaDeArchivoException(string mensaje) : base(mensaje)
    {
    }
}
