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

    protected override void ValidarSolicitud(SolicitudRecibida solicitud)
        => solicitud.ExigirDatosVacios();

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        IReadOnlyList<LecturaDeReserva> reservas = _gestionDeReservas.ConsultarReservasDe(usuario);
        IReadOnlyList<ReservaListada> listado = reservas.Select(AListada).ToList();
        return RespuestaDelServidor.Ok(CodecDeConsultaDeReservas.CodificarResultado(listado));
    }

    private static ReservaListada AListada(LecturaDeReserva reserva)
        => new(
            reserva.Id,
            reserva.NombreModelo,
            reserva.Estado,
            reserva.FechaCreacion,
            reserva.UltimaActualizacion);
}
