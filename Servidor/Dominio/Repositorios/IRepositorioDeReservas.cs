using Servidor.Dominio.Entidades;

namespace Servidor.Dominio.Repositorios;

// Puerto de persistencia de reservas. Crear y cambiar de estado dejan una actividad en el
// historial (o ninguna, en un rechazo), y se guardan juntas en una transacción.
public interface IRepositorioDeReservas
{
    Task AgregarAsync(Reserva reserva, Actividad actividad);

    Task<Reserva?> BuscarAsync(Guid idReserva);

    Task ActualizarEstadoAsync(Reserva reserva, Actividad? actividad);

    Task<IReadOnlyList<Reserva>> ListarDeAsync(string nombreUsuario);

    Task<IReadOnlyList<Reserva>> ListarPendientesAsync();

    Task<int> ContarConfirmadasAsync(int idModelo);

    // Pendientes más confirmadas.
    Task<int> ContarActivasAsync(int idModelo);
}
