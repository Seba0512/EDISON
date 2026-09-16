using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

/// <summary>
/// Operación remota de CMD 30 (CR10): pide el historial del usuario autenticado y decodifica la
/// lista de <see cref="ActividadListada"/>. El REQ va vacío, igual que el cierre de sesión: el
/// usuario sale de la sesión de la conexión, no de ningún dato que arme el cliente.
/// </summary>
internal sealed class HistorialRemota
{
    private readonly ConexionConServidor _conexion;

    public HistorialRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public IReadOnlyList<ActividadListada> Ejecutar()
    {
        byte[] cuerpo = _conexion.EnviarSolicitudYLeerCuerpo(Comandos.ConsultarHistorial, []);
        return CodecDeHistorial.DecodificarResultado(cuerpo);
    }
}
