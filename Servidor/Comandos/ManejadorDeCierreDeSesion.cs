using Common.Protocolo;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

internal sealed class ManejadorDeCierreDeSesion : ManejadorAutenticado
{
    private readonly IAutenticacion _autenticacion;

    public ManejadorDeCierreDeSesion(IAutenticacion autenticacion)
    {
        _autenticacion = autenticacion;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        _autenticacion.CerrarSesion(usuario);
        solicitud.Sesion.Cerrar();
        return RespuestaDelServidor.Ok("Sesión cerrada");
    }
}
