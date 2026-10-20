using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;
using Servidor.Dominio.Repositorios;

namespace Servidor.Dominio.Servicios;

// Ciclo de vida de las reservas: solicitud, resolución diferida y cancelación.
public class ServicioDeReservas
{
    private readonly IRepositorioDeReservas _reservas;
    private readonly IRepositorioDeModelos _modelos;
    private readonly CandadoGlobal _candado;
    // Toda reserva se resuelve este tiempo después de pedirla. Llega por configuración.
    private readonly TimeSpan _plazoDeResolucion;

    public ServicioDeReservas(
        IRepositorioDeReservas reservas,
        IRepositorioDeModelos modelos,
        CandadoGlobal candado,
        TimeSpan plazoDeResolucion)
    {
        _reservas = reservas;
        _modelos = modelos;
        _candado = candado;
        _plazoDeResolucion = plazoDeResolucion;
    }

    public async Task<Guid> SolicitarReservaAsync(string nombreModelo, Usuario solicitante)
    {
        Reserva reserva;
        await _candado.TomarAsync();
        try
        {
            VehiculoModelo modelo = await _modelos.ObtenerAsync(nombreModelo);
            reserva = new Reserva(solicitante, modelo);
            await _reservas.AgregarAsync(reserva, Actividad.ReservaCreada(modelo.Nombre));
        }
        finally
        {
            _candado.Liberar();
        }

        // La espera es un Task.Delay y no ocupa ningún hilo mientras dura. Nadie espera este
        // Task: ResolverTrasDemoraAsync registra sus propios errores.
        _ = ResolverTrasDemoraAsync(reserva.Id);
        return reserva.Id;
    }

    public async Task CancelarReservaAsync(Guid idReserva, Usuario solicitante)
    {
        await _candado.TomarAsync();
        try
        {
            Reserva reserva = await _reservas.BuscarAsync(idReserva)
                ?? throw new DominioException($"No existe la reserva {idReserva}");
            if (!reserva.FueHechaPor(solicitante.NombreUsuario))
            {
                throw new DominioException("Solo se pueden cancelar las reservas propias");
            }

            reserva.Cancelar();
            await _reservas.ActualizarEstadoAsync(
                reserva, Actividad.ReservaCancelada(reserva.NombreDelModelo()));
        }
        finally
        {
            _candado.Liberar();
        }
    }

    public async Task<IReadOnlyList<LecturaDeReserva>> ConsultarReservasDeAsync(Usuario usuario)
    {
        await _candado.TomarAsync();
        try
        {
            return (await _reservas.ListarDeAsync(usuario.NombreUsuario))
                .Select(reserva => reserva.TomarLectura())
                .ToList();
        }
        finally
        {
            _candado.Liberar();
        }
    }

    private async Task ResolverTrasDemoraAsync(Guid idReserva)
    {
        try
        {
            await Task.Delay(_plazoDeResolucion);
            await ResolverReservaAsync(idReserva);
        }
        catch (Exception exception)
        {
            // Nadie observa este Task, así que sin este catch la excepción se perdería en silencio
            // y la reserva quedaría Pendiente.
            Console.Error.WriteLine($"No se pudo resolver la reserva {idReserva}: {exception}");
        }
    }

    private async Task ResolverReservaAsync(Guid idReserva)
    {
        await _candado.TomarAsync();
        try
        {
            Reserva? reserva = await _reservas.BuscarAsync(idReserva);
            // Si la cancelaron antes de que venciera el plazo, ya no hay nada que resolver.
            if (reserva is null || !reserva.EstaPendiente)
            {
                return;
            }

            VehiculoModelo modelo = await _modelos.BuscarPorIdAsync(reserva.ModeloId!.Value)
                ?? throw new InvalidOperationException(
                    $"La reserva pendiente {idReserva} apunta a un modelo que no existe");
            int disponibles = modelo.Stock - await _reservas.ContarConfirmadasAsync(modelo.Id);
            if (disponibles > 0)
            {
                reserva.Confirmar();
                // Solo una confirmación cuenta como compra en el historial. Un rechazo no registra nada.
                await _reservas.ActualizarEstadoAsync(
                    reserva, Actividad.Compra(reserva.NombreDelModelo()));
            }
            else
            {
                reserva.Rechazar();
                await _reservas.ActualizarEstadoAsync(reserva, null);
            }
        }
        finally
        {
            _candado.Liberar();
        }
    }
}
