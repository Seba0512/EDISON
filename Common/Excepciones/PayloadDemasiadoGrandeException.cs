namespace Common.Excepciones;

// Se lanza al ESCRIBIR una trama que supera el máximo admitido, antes de tocar el stream de red.
public sealed class PayloadDemasiadoGrandeException : ProtocoloException
{
    public PayloadDemasiadoGrandeException(string mensaje) : base(mensaje)
    {
    }
}
