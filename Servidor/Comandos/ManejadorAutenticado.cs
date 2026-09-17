using Common.Protocolo;
using Dominio;

namespace Servidor.Comandos;

internal abstract class ManejadorAutenticado : IManejadorDeComando
{
    public const string SesionRequerida = "Debe iniciar sesión para realizar esta operación";

    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
    {
        ValidarSolicitud(solicitud);
        if (!solicitud.Sesion.TieneSesion)
            return RespuestaDelServidor.Rechazo(SesionRequerida);

        return ResponderAutenticado(solicitud, solicitud.Sesion.UsuarioAutenticado);
    }

    protected virtual void ValidarSolicitud(SolicitudRecibida solicitud)
    {
    }

    protected abstract RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario);
}
