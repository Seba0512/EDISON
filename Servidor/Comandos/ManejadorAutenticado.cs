using Common.Protocolo;
using Dominio;

namespace Servidor.Comandos;

internal abstract class ManejadorAutenticado : IManejadorDeComando
{
    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
        => ResponderAutenticado(solicitud, solicitud.Sesion.UsuarioAutenticado);

    protected abstract RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario);
}
