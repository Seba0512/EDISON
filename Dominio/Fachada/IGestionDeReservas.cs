namespace Dominio.Fachada;

/// <summary>
/// Solicitud, cancelacion y consulta de reservas (CR5, CR6, CR9). La usan los manejadores 20 a 22.
/// </summary>
public interface IGestionDeReservas
{
    /// <summary>Registra una solicitud, que nace pendiente (CR5), y devuelve su id.</summary>
    Guid SolicitarReserva(string nombreModelo, Usuario solicitante);

    void CancelarReserva(Guid idReserva, Usuario solicitante);

    /// <summary>Reservas del usuario para CR9. Coleccion vacia si no tiene ninguna.</summary>
    IReadOnlyList<Reserva> ReservasDe(Usuario usuario);
}
