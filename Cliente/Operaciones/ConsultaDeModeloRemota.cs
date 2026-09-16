using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

internal sealed class ConsultaDeModeloRemota
{
    private readonly ConexionConServidor _conexion;

    public ConsultaDeModeloRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public DetalleDeModelo Ejecutar(string nombreModelo)
    {
        byte[] solicitud = CodecDeConsultaDeModelo.CodificarSolicitud(nombreModelo);
        byte[] cuerpo = _conexion.EnviarSolicitudYLeerCuerpo(Comandos.ConsultarModelo, solicitud);
        return CodecDeConsultaDeModelo.DecodificarResultado(cuerpo);
    }
}
