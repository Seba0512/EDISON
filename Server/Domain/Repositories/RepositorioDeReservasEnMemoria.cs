using Server.Domain.Entities;
using Server.Domain.Exceptions;

namespace Server.Domain.Repositories;

public class RepositorioDeReservasEnMemoria : IRepositorioDeReservas
{
    private readonly Dictionary<Guid, Reserva> _reservas = new();
    private readonly object _candado = new();

    public void Agregar(Reserva reserva)
    {
        lock (_candado)
        {
            _reservas.Add(reserva.Id, reserva);
        }
    }

    public Reserva Obtener(Guid idReserva)
    {
        lock (_candado)
        {
            if (!_reservas.TryGetValue(idReserva, out Reserva? reserva))
            {
                throw new DominioException($"No existe la reserva {idReserva}");
            }

            return reserva;
        }
    }

    public IReadOnlyList<Reserva> Todas()
    {
        lock (_candado)
        {
            return _reservas.Values.ToList();
        }
    }
}
