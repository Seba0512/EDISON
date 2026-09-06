namespace Dominio.Excepciones;

/// <summary>
/// Transicion de estado que la reserva no permite (CR5, CR6): confirmar una cancelada, cancelar una
/// rechazada, etc. Es una <see cref="DominioException"/> porque representa una regla de negocio.
/// </summary>
public class TransicionDeReservaInvalidaException : DominioException
{
    public TransicionDeReservaInvalidaException(string operacion, string nombreDelEstado)
        : base($"No se puede {operacion} una reserva {nombreDelEstado}")
    {
    }
}
