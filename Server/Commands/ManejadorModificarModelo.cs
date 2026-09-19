using Common;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorModificarModelo : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorModificarModelo(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.ModificarModelo;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        _edison.ModificarModelo(
            FichaDeModeloRecibida.De(solicitud), conexion.Sesion.UsuarioAutenticado);
        return RespuestaDelServidor.Ok("Modelo modificado correctamente.");
    }
}
