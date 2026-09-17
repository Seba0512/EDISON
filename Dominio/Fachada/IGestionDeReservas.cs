namespace Dominio.Fachada;

public interface IGestionDeReservas
{
    Guid SolicitarReserva(string nombreModelo, Usuario solicitante);

    void CancelarReserva(Guid idReserva, Usuario solicitante);

    IReadOnlyList<LecturaDeReserva> ConsultarReservasDe(Usuario usuario);
}
