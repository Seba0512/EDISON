namespace Dominio.Excepciones;

/// <summary>
/// Transicion de estado que la reserva no permite (CR5, CR6): confirmar una cancelada, cancelar una
/// rechazada, etc. Es una <see cref="DominioException"/> para que el borde del servidor la trate
/// como cualquier otra regla de negocio: responde al cliente y sigue vivo.
/// </summary>
public class TransicionDeReservaInvalidaException : DominioException
{
    public TransicionDeReservaInvalidaException(string operacion, string nombreDelEstado)
        : base($"No se puede {operacion} una reserva {nombreDelEstado}")
    {
    }
}
