using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Dominio.Tests.Dobles;

/// <summary>Indice de reservas en memoria para las pruebas, por id y por usuario.</summary>
public class RepositorioDeReservasEnMemoria : IRepositorioDeReservas
{
    private readonly Dictionary<Guid, Reserva> reservas = new();

    public void Agregar(Reserva reserva)
    {
        reservas[reserva.Id] = reserva;
    }

    public Reserva ObtenerPorId(Guid idReserva)
    {
        if (!reservas.TryGetValue(idReserva, out Reserva? reserva))
        {
            throw new DominioException($"No existe la reserva {idReserva}");
        }

        return reserva;
    }

    public IReadOnlyList<Reserva> DeUsuario(string nombreUsuario)
    {
        return reservas.Values.Where(reserva => reserva.FueHechaPor(nombreUsuario)).ToList();
    }

    public IReadOnlyList<Reserva> Pendientes()
    {
        return reservas.Values.Where(reserva => reserva.EstaPendiente).ToList();
    }
}
