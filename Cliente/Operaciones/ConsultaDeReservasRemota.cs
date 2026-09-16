using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

internal sealed class ConsultaDeReservasRemota
{
    private readonly ConexionConServidor _conexion;

    public ConsultaDeReservasRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public IReadOnlyList<ReservaListada> Ejecutar()
    {
        byte[] cuerpo = _conexion.EnviarSolicitudYLeerCuerpo(Comandos.ConsultarMisReservas, []);
        return CodecDeConsultaDeReservas.DecodificarResultado(cuerpo);
    }
}
