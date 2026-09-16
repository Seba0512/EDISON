using Common.Protocolo;

namespace Cliente.Operaciones;

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
