using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;

namespace Servidor.Dominio.Servicios;

// Ciclo de vida de las reservas: solicitud, resolución diferida y cancelación.
public class ServicioDeReservas
{
    private readonly EstadoEnMemoria _estado;
    // Toda reserva se resuelve este tiempo después de pedirla. Llega por configuración.
    private readonly TimeSpan _plazoDeResolucion;

    public ServicioDeReservas(EstadoEnMemoria estado, TimeSpan plazoDeResolucion)
    {
        _estado = estado;
        _plazoDeResolucion = plazoDeResolucion;
    }

    public async Task<Guid> SolicitarReservaAsync(string nombreModelo, Usuario solicitante)
    {
        Reserva reserva;
        await _estado.Candado.WaitAsync();
        try
        {
            VehiculoModelo modelo = _estado.ObtenerModelo(nombreModelo);
            reserva = new Reserva(solicitante, modelo);
            _estado.Reservas.Add(reserva.Id, reserva);
            solicitante.Registrar(Actividad.ReservaCreada(modelo.Nombre));
        }
        finally
        {
            _estado.Candado.Release();
        }

        // La espera es un Task.Delay y no ocupa ningún hilo mientras dura. Nadie espera este
        // Task: ResolverTrasDemoraAsync registra sus propios errores.
        _ = ResolverTrasDemoraAsync(reserva.Id);
        return reserva.Id;
    }

    public async Task CancelarReservaAsync(Guid idReserva, Usuario solicitante)
    {
        await _estado.Candado.WaitAsync();
        try
        {
            Reserva reserva = _estado.ObtenerReserva(idReserva);
            if (!reserva.FueHechaPor(solicitante.NombreUsuario))
            {
                throw new DominioException("Solo se pueden cancelar las reservas propias");
            }

            reserva.Cancelar();
            solicitante.Registrar(Actividad.ReservaCancelada(reserva.NombreDelModelo()));
        }
        finally
        {
            _estado.Candado.Release();
        }
    }

    public async Task<IReadOnlyList<LecturaDeReserva>> ConsultarReservasDeAsync(Usuario usuario)
    {
        await _estado.Candado.WaitAsync();
        try
        {
            return _estado.Reservas.Values
                .Where(reserva => reserva.FueHechaPor(usuario.NombreUsuario))
                .Select(reserva => reserva.TomarLectura())
                .ToList();
        }
        finally
        {
            _estado.Candado.Release();
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
        await _estado.Candado.WaitAsync();
        try
        {
            // Si la cancelaron antes de que venciera el plazo, ya no hay nada que resolver.
            if (!_estado.Reservas.TryGetValue(idReserva, out Reserva? reserva) || !reserva.EstaPendiente)
            {
                return;
            }

            int disponibles = reserva.Modelo.Stock - _estado.ContarConfirmadas(reserva.Modelo);
            if (disponibles > 0)
            {
                reserva.Confirmar();
                // Solo una confirmación cuenta como compra en el historial. Un rechazo no registra nada.
                reserva.Solicitante.Registrar(Actividad.Compra(reserva.NombreDelModelo()));
            }
            else
            {
                reserva.Rechazar();
            }
        }
        finally
        {
            _estado.Candado.Release();
        }
    }
}
