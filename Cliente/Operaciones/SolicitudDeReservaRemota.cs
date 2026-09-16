using Common.Protocolo;

namespace Cliente.Operaciones;

/// <summary>
/// CMD 20 del lado del cliente (CR5): arma el pedido con el nombre del modelo, lo envía por
/// <see cref="ConexionConServidor"/> y devuelve el texto de la respuesta OK (que trae el Guid de la
/// reserva). Aísla a la opción de menú de bytes y sockets.
/// </summary>
internal sealed class SolicitudDeReservaRemota
{
    private readonly ConexionConServidor _conexion;

    public SolicitudDeReservaRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public string Ejecutar(string nombreModelo)
        => _conexion.EnviarSolicitud(Comandos.SolicitarReserva, CodecDeSolicitudDeReserva.Codificar(nombreModelo));
}
