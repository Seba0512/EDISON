namespace Common;

public class ProtocoloException : Exception
{
    public ProtocoloException(string mensaje) : base(mensaje)
    {
    }

    public ProtocoloException(string mensaje, Exception causaOriginal) : base(mensaje, causaOriginal)
    {
    }
}

// El otro extremo cerró la conexión de forma ordenada entre una trama y la siguiente.
public sealed class ConexionCerradaException : Exception
{
    public ConexionCerradaException() : base("La conexión fue cerrada por el otro extremo")
    {
    }
}

// La conexión se cortó a mitad de una trama: lo recibido queda incompleto y no se puede usar.
public sealed class TramaTruncadaException : ProtocoloException
{
    public TramaTruncadaException(string campo)
        : base($"La conexión se cerró a mitad del campo {campo}")
    {
    }
}

// Una respuesta supera el tamaño máximo. Se detecta antes de escribir, así la conexión sigue viva.
public sealed class PayloadDemasiadoGrandeException : ProtocoloException
{
    public PayloadDemasiadoGrandeException(int largo, int maximo)
        : base($"El payload de {largo} bytes supera el máximo de {maximo}")
    {
    }
}
