using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

/// <summary>
/// CMD 30: consulta del historial de actividades del usuario autenticado (CR10). El REQ va vacío,
/// igual que <see cref="ManejadorDeCierreDeSesion"/>: el usuario sale de la sesión, no del payload.
/// Depende de <see cref="IHistorialDeUsuario"/> y no de <c>Edison</c> entero (ISP/DIP).
/// </summary>
internal sealed class ManejadorDeHistorial : ManejadorAutenticado
{
    private readonly IHistorialDeUsuario _historialDeUsuario;

    public ManejadorDeHistorial(IHistorialDeUsuario historialDeUsuario)
    {
        _historialDeUsuario = historialDeUsuario;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        IReadOnlyList<Actividad> historial = _historialDeUsuario.HistorialDe(usuario);
        List<ActividadListada> actividades = historial
            .Select(actividad => new ActividadListada(
                actividad.Tipo.ToString(), actividad.Detalle, actividad.FechaHora))
            .ToList();

        return RespuestaDelServidor.Ok(CodecDeHistorial.CodificarResultado(actividades));
    }
}
