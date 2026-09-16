using Common.Protocolo;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

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
