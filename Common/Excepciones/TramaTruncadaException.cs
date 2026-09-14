namespace Common.Excepciones;

public sealed class TramaTruncadaException : ProtocoloException
{
    public TramaTruncadaException(string campo)
        : base($"La conexión se cerró antes de completar {campo}")
    {
    }
}
