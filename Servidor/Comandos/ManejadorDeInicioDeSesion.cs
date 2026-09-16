using Common.Protocolo;
using Dominio;
using Dominio.Excepciones;
using Dominio.Fachada;
using Servidor.Red;

namespace Servidor.Comandos;

internal sealed class ManejadorDeInicioDeSesion : IManejadorDeComando
{
    private readonly IAutenticacion _autenticacion;

    public ManejadorDeInicioDeSesion(IAutenticacion autenticacion)
    {
        _autenticacion = autenticacion;
    }

    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
    {
        ExigirSinSesion(solicitud.Sesion);
        Credenciales credenciales = CodecDeCredenciales.Decodificar(solicitud.Datos.Span);
        Usuario usuario = _autenticacion.IniciarSesion(credenciales.NombreUsuario, credenciales.Clave);
        solicitud.Sesion.Autenticar(usuario);
        return RespuestaDelServidor.Ok($"Bienvenido, {usuario.NombreUsuario}");
    }

    private static void ExigirSinSesion(EstadoDeSesion sesion)
    {
        if (sesion.TieneSesion)
            throw new DominioException("La conexión ya tiene una sesión iniciada");
    }
}
