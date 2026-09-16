using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

internal sealed class ManejadorDeConsultaDeReservas : ManejadorAutenticado
{
    private readonly IGestionDeReservas _gestionDeReservas;

    public ManejadorDeConsultaDeReservas(IGestionDeReservas gestionDeReservas)
    {
        _gestionDeReservas = gestionDeReservas;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        IReadOnlyList<Reserva> reservas = _gestionDeReservas.ReservasDe(usuario);
        IReadOnlyList<ReservaListada> listado = reservas.Select(AListada).ToList();
        return RespuestaDelServidor.Ok(CodecDeConsultaDeReservas.CodificarResultado(listado));
    }

    private static ReservaListada AListada(Reserva reserva)
        => new(
            reserva.Id,
            reserva.NombreDelModelo(),
            reserva.NombreDelEstado(),
            reserva.FechaCreacion,
            reserva.UltimaActualizacion);
}
