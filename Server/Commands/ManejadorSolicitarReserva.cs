using Common;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorSolicitarReserva : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorSolicitarReserva(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.SolicitarReserva;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        Guid idReserva = _edison.SolicitarReserva(
            solicitud.DatosComoTexto(), conexion.Sesion.UsuarioAutenticado);
        return RespuestaDelServidor.Ok(
            $"Reserva {FormatoDeCampos.De(idReserva)} creada, pendiente de aprobación");
    }
}
