namespace Common.Excepciones;

public class ProtocoloException : Exception
{
    public ProtocoloException(string mensaje) : base(mensaje)
    {
    }

    public ProtocoloException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
