using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

/// <summary>
/// CMD 14 del lado del cliente (CR8): arma el pedido, lo envía por <see cref="ConexionConServidor"/>
/// y decodifica la ficha del modelo devuelta. Aísla a la opción de menú de bytes, sockets y del
/// formato exacto del payload.
/// </summary>
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
