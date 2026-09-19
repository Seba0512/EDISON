using Server.Domain.Entities;

namespace Server.Domain.Repositories;

public interface IRepositorioDeReservas
{
    void Agregar(Reserva reserva);

    Reserva Obtener(Guid idReserva);

    IReadOnlyList<Reserva> Todas();
}
