using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Dominio.Tests.Dobles;

public class RepositorioDeReservasEnMemoria : IRepositorioDeReservas
{
    private readonly object candado = new();
    private readonly Dictionary<Guid, Reserva> reservas = new();

    public void Agregar(Reserva reserva)
    {
        Guid id = reserva.Id;
        lock (candado)
        {
            if (reservas.ContainsKey(id))
            {
                throw new DominioException($"Ya existe la reserva {id}");
            }

            reservas.Add(id, reserva);
        }
    }

    public Reserva ObtenerPorId(Guid idReserva)
    {
        lock (candado)
        {
            if (!reservas.TryGetValue(idReserva, out Reserva? reserva))
            {
                throw new DominioException($"No existe la reserva {idReserva}");
            }

            return reserva;
        }
    }

    public IReadOnlyList<Reserva> DeUsuario(string nombreUsuario)
    {
        return Snapshot().Where(reserva => reserva.FueHechaPor(nombreUsuario)).ToList();
    }

    public IReadOnlyList<Reserva> Pendientes()
    {
        return Snapshot().Where(reserva => reserva.EstaPendiente).ToList();
    }

    private List<Reserva> Snapshot()
    {
        lock (candado)
        {
            return reservas.Values.ToList();
        }
    }
}
