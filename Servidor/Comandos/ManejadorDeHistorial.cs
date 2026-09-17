using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

internal sealed class ManejadorDeHistorial : ManejadorAutenticado
{
    private readonly IHistorialDeUsuario _historialDeUsuario;

    public ManejadorDeHistorial(IHistorialDeUsuario historialDeUsuario)
    {
        _historialDeUsuario = historialDeUsuario;
    }

    protected override void ValidarSolicitud(SolicitudRecibida solicitud)
        => solicitud.ExigirDatosVacios();

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
