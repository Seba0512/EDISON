using Common;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorCerrarSesion : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorCerrarSesion(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.CerrarSesion;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        ExigirDatosVacios(solicitud);
        _edison.CerrarSesion(conexion.Sesion.UsuarioAutenticado);
        conexion.Sesion.Cerrar();
        return RespuestaDelServidor.Ok("Sesión cerrada");
    }
}
