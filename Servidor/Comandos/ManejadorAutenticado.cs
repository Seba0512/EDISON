using Common.Protocolo;
using Dominio;

namespace Servidor.Comandos;

internal abstract class ManejadorAutenticado : IManejadorDeComando
{
    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
    {
        if (!solicitud.Sesion.TieneSesion)
            return RespuestaDelServidor.Rechazo("Debe iniciar sesión para realizar esta operación");

        return ResponderAutenticado(solicitud, solicitud.Sesion.UsuarioAutenticado);
    }

    protected abstract RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario);
}
