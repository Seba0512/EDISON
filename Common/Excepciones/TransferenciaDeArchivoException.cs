namespace Common.Excepciones;

/// <summary>
/// Fallo durante la transferencia binaria fuera de frame de un archivo (CMD 15/16). No hereda de
/// <see cref="ProtocoloException"/> a propósito: a mitad de archivo el stream queda con bytes que ya
/// no corresponden a ningún frame, así que el fallo debe ser fatal para esa conexión y no un PR del
/// que se pueda seguir conversando.
/// </summary>
public sealed class TransferenciaDeArchivoException : Exception
{
    public TransferenciaDeArchivoException(string mensaje) : base(mensaje)
    {
    }
}
