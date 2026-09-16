namespace Dominio.Excepciones;

public class TransicionDeReservaInvalidaException : DominioException
{
    public TransicionDeReservaInvalidaException(string operacion, string nombreDelEstado)
        : base($"No se puede {operacion} una reserva {nombreDelEstado}")
    {
    }
}
