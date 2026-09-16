using Common.Protocolo;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

/// <summary>
/// CMD 20: solicitud de reserva (CR5). Exige sesión iniciada vía <see cref="ManejadorAutenticado"/>.
/// La reserva nace pendiente: este manejador no decide si hay stock o no, eso lo resuelve después la
/// mesa de aprobaciones de forma diferida. Devuelve el Guid de la reserva creada como texto plano,
/// igual que el resto de las respuestas OK del protocolo.
/// </summary>
internal sealed class ManejadorDeSolicitudDeReserva : ManejadorAutenticado
{
    private readonly IGestionDeReservas _gestionDeReservas;

    public ManejadorDeSolicitudDeReserva(IGestionDeReservas gestionDeReservas)
    {
        _gestionDeReservas = gestionDeReservas;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        string nombreModelo = CodecDeSolicitudDeReserva.Decodificar(solicitud.Datos.Span);
        Guid idReserva = _gestionDeReservas.SolicitarReserva(nombreModelo, usuario);

        return RespuestaDelServidor.Ok(
            $"Reserva {FormatoDeCampos.De(idReserva)} creada, pendiente de aprobación");
    }
}
