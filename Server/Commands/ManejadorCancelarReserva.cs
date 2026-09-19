using Common;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorCancelarReserva : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorCancelarReserva(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.CancelarReserva;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        Guid idReserva = FormatoDeCampos.AGuid(solicitud.DatosComoTexto());
        _edison.CancelarReserva(idReserva, conexion.Sesion.UsuarioAutenticado);
        return RespuestaDelServidor.Ok($"Reserva '{idReserva}' cancelada");
    }
}
