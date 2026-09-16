using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

/// <summary>
/// CMD 22: consulta de las reservas del usuario autenticado (CR9). El REQ no lleva campos (el
/// usuario sale de la sesion, igual que <see cref="ManejadorDeCierreDeSesion"/>), asi que no hace
/// falta instanciar un <see cref="LectorDeCampos"/> para decodificarlo: un payload vacio ya cumple
/// <c>ExigirFinDePayload</c> por construccion. <see cref="IGestionDeReservas"/> es quien sabe listar
/// las reservas de un usuario (Experto en informacion); este manejador solo mapea cada
/// <see cref="Reserva"/> a la vista de protocolo <see cref="ReservaListada"/> y codifica el resultado.
/// </summary>
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
