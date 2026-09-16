using Common.Protocolo;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

/// <summary>
/// CMD 21: cancelación de una reserva propia (CR6). Decodifica el id pedido y delega todo en
/// <see cref="IGestionDeReservas.CancelarReserva"/>, que ya exige que la reserva exista, sea del
/// solicitante y todavía pueda cancelarse (lanzando <see cref="Dominio.Excepciones.DominioException"/>
/// si no, traducida sola a ER por <see cref="DespachadorDeComandos"/>). El manejador no conoce esas
/// reglas: sólo traduce protocolo a dominio y viceversa.
/// </summary>
internal sealed class ManejadorDeCancelacionDeReserva : ManejadorAutenticado
{
    private readonly IGestionDeReservas _gestionDeReservas;

    public ManejadorDeCancelacionDeReserva(IGestionDeReservas gestionDeReservas)
    {
        _gestionDeReservas = gestionDeReservas;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        Guid idReserva = CodecDeCancelacionDeReserva.Decodificar(solicitud.Datos.Span);
        _gestionDeReservas.CancelarReserva(idReserva, usuario);
        return RespuestaDelServidor.Ok($"Reserva '{idReserva}' cancelada");
    }
}
