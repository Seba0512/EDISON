using Common.Protocolo;

namespace Cliente.Operaciones;

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
