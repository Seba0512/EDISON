using Common.Protocolo;
using Dominio;
using Servidor.Red;

namespace Servidor.Comandos;

/// <summary>
/// Esqueleto fijo (Template Method) de los comandos que exigen sesión iniciada: obtiene el
/// <see cref="Usuario"/> autenticado de <see cref="EstadoDeSesion"/> (que lanza si no hay sesión) y
/// delega el resto en la subclase concreta.
/// </summary>
internal abstract class ManejadorAutenticado : IManejadorDeComando
{
    public RespuestaDelServidor Responder(ReadOnlyMemory<byte> datos, EstadoDeSesion sesion)
        => ResponderAutenticado(datos, sesion.UsuarioAutenticado, sesion);

    protected abstract RespuestaDelServidor ResponderAutenticado(
        ReadOnlyMemory<byte> datos, Usuario usuario, EstadoDeSesion sesion);
}
