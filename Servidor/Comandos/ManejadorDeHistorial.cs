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
        IReadOnlyList<ActividadListada> actividades = historial.Select(AVista).ToList();
        return RespuestaDelServidor.Ok(CodecDeHistorial.CodificarResultado(actividades));
    }

    private static ActividadListada AVista(Actividad actividad)
        => new(actividad.Tipo.ToString(), actividad.Detalle, actividad.FechaHora);
}
