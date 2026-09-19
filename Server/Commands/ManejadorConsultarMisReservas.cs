using Common;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorConsultarMisReservas : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorConsultarMisReservas(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.ConsultarMisReservas;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        ExigirDatosVacios(solicitud);
        List<ReservaListada> listado = _edison
            .ConsultarReservasDe(conexion.Sesion.UsuarioAutenticado)
            .Select(reserva => new ReservaListada(
                reserva.Id,
                reserva.NombreModelo,
                reserva.Estado.ToString(),
                reserva.FechaCreacion,
                reserva.UltimaActualizacion))
            .ToList();

        return RespuestaDelServidor.Ok(Mensajes.DeReservasListadas(listado));
    }
}
