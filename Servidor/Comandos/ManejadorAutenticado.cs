using Common.Protocolo;
using Dominio;

namespace Servidor.Comandos;

/// <summary>
/// Esqueleto fijo (Template Method) de los comandos que exigen sesión iniciada: obtiene el
/// <see cref="Usuario"/> autenticado de la sesión (que lanza si no hay sesión) y delega el resto en
/// la subclase concreta.
/// </summary>
internal abstract class ManejadorAutenticado : IManejadorDeComando
{
    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
        => ResponderAutenticado(solicitud, solicitud.Sesion.UsuarioAutenticado);

    protected abstract RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario);
}
