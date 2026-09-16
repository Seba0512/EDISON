using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Servidor.Repositorios;

/// <summary>
/// Implementación productiva de <see cref="IRepositorioDeReservas"/> (CR5, CR6, CR9): índice de
/// reservas en memoria. Portado tal cual desde el doble ya validado por
/// <c>RepositoriosConcurrentesTests</c> en <c>Dominio.Tests</c>.
/// </summary>
internal sealed class RepositorioDeReservasEnMemoria : IRepositorioDeReservas
{
    private readonly object _candado = new();
    private readonly Dictionary<Guid, Reserva> _reservas = new();

    public void Agregar(Reserva reserva)
    {
        Guid id = reserva.Id;
        lock (_candado)
        {
            if (_reservas.ContainsKey(id))
                throw new DominioException($"Ya existe la reserva {id}");

            _reservas.Add(id, reserva);
        }
    }

    public Reserva ObtenerPorId(Guid idReserva)
    {
        lock (_candado)
        {
            if (!_reservas.TryGetValue(idReserva, out Reserva? reserva))
                throw new DominioException($"No existe la reserva {idReserva}");

            return reserva;
        }
    }

    public IReadOnlyList<Reserva> DeUsuario(string nombreUsuario)
        => Snapshot().Where(reserva => reserva.FueHechaPor(nombreUsuario)).ToList();

    public IReadOnlyList<Reserva> Pendientes()
        => Snapshot().Where(reserva => reserva.EstaPendiente).ToList();

    private List<Reserva> Snapshot()
    {
        lock (_candado)
            return _reservas.Values.ToList();
    }
}
