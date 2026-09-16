using Common.Protocolo;

namespace Cliente.Operaciones;

/// <summary>
/// CMD 21 del lado del cliente (CR6): arma el pedido con el id de la reserva, lo envía por
/// <see cref="ConexionConServidor"/> y devuelve el texto de la respuesta (confirmación u error, ya
/// resuelto por <see cref="ConexionConServidor.EnviarSolicitud"/>). Aísla a la opción de menú de
/// bytes, sockets y del formato exacto del payload.
/// </summary>
internal sealed class CancelacionDeReservaRemota
{
    private readonly ConexionConServidor _conexion;

    public CancelacionDeReservaRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public string Ejecutar(Guid idReserva)
    {
        byte[] solicitud = CodecDeCancelacionDeReserva.Codificar(idReserva);
        return _conexion.EnviarSolicitud(Comandos.CancelarReserva, solicitud);
    }
}
