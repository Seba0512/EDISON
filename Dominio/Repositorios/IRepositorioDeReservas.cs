namespace Dominio.Repositorios;

public interface IRepositorioDeReservas
{
    void Agregar(Reserva reserva);

    Reserva ObtenerPorId(Guid idReserva);

    IReadOnlyList<Reserva> DeUsuario(string nombreUsuario);

    IReadOnlyList<Reserva> Pendientes();
}
