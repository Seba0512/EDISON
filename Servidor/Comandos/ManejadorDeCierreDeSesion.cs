using Common.Protocolo;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

/// <summary>CMD 03: cierre de sesión (CR1). Exige sesión iniciada vía <see cref="ManejadorAutenticado"/>.</summary>
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
