using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

/// <summary>
/// CMD 22 del lado cliente (CR9): unica responsabilidad, pedirle a la conexion las reservas del
/// usuario autenticado y decodificar la respuesta. Es la Indireccion entre la opcion de menu y el
/// protocolo binario: <see cref="Cliente.Menu.Opciones.OpcionMisReservas"/> no ve bytes ni sockets.
/// </summary>
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
