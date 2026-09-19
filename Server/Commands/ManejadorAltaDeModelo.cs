using Common;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorAltaDeModelo : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorAltaDeModelo(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.AltaDeModelo;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        _edison.AltaDeModelo(
            FichaDeModeloRecibida.De(solicitud), conexion.Sesion.UsuarioAutenticado);
        return RespuestaDelServidor.Ok("Modelo creado correctamente.");
    }
}
