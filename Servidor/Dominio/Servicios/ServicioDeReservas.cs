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
    private readonly IGeneradorDeComprobantes _comprobantes;
    private readonly ResolutorDeReservas _resolutor;
    // Toda reserva se resuelve este tiempo después de pedirla. Llega por configuración.
    private readonly TimeSpan _plazoDeResolucion;
    private readonly CancellationToken _apagado;

    public ServicioDeReservas(
        IRepositorioDeReservas reservas,
        IRepositorioDeModelos modelos,
        CandadoGlobal candado,
        IGeneradorDeComprobantes comprobantes,
        TimeSpan plazoDeResolucion,
        CancellationToken apagado)
    {
        _reservas = reservas;
        _modelos = modelos;
        _candado = candado;
        _comprobantes = comprobantes;
        _plazoDeResolucion = plazoDeResolucion;
        _apagado = apagado;
        _resolutor = new ResolutorDeReservas(ResolverReservaAsync, apagado);
    }

    // Con la base persistente, un reinicio no puede dejar reservas pendientes para siempre: las
    // que quedaron esperando se reprograman con la demora que les falte, en UTC.
    public async Task ReprogramarPendientesAsync()
    {
        IReadOnlyList<Reserva> pendientes = await _reservas.ListarPendientesAsync();
        foreach (Reserva reserva in pendientes)
        {
            TimeSpan demora = reserva.FechaCreacion + _plazoDeResolucion - DateTime.UtcNow;
            _resolutor.Programar(reserva.Id, demora > TimeSpan.Zero ? demora : TimeSpan.Zero);
        }

        if (pendientes.Count > 0)
        {
            Console.WriteLine($"{pendientes.Count} reserva(s) pendiente(s) reprogramada(s).");
        }
    }

    // Parte del cierre: con el token de apagado ya cancelado, espera con un tope a las reservas que
    // estaban confirmándose, para que el proceso no termine con un comprobante a medio escribir.
    public async Task EsperarResolucionesEnCursoAsync(TimeSpan esperaMaxima)
    {
        try
        {
            await _resolutor.EsperarResolucionesEnCursoAsync().WaitAsync(esperaMaxima);
        }
        catch (TimeoutException)
        {
            Console.Error.WriteLine("Hay reservas que no terminaron de resolverse antes del cierre.");
        }
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

        // La espera es un Task.Delay cancelable y no ocupa ningún hilo mientras dura.
        _resolutor.Programar(reserva.Id, _plazoDeResolucion);
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

            bool estabaPendiente = reserva.EstaPendiente;
            reserva.Cancelar();
            await _reservas.ActualizarEstadoAsync(
                reserva, Actividad.ReservaCancelada(reserva.NombreDelModelo()));
            if (estabaPendiente)
            {
                // La espera se corta recién con la cancelación guardada: si la base falla, la reserva
                // sigue Pendiente y su espera sigue viva para resolverla. Si la resolución ya había
                // arrancado, espera este candado y al entrar ve que la reserva dejó de estar pendiente.
                _resolutor.Cancelar(idReserva);
            }
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

    private async Task ResolverReservaAsync(Guid idReserva)
    {
        LecturaDeReserva? confirmada = null;
        decimal precio = 0;
        // Política de apagado: una resolución que todavía espera el candado se abandona (la reserva
        // queda Pendiente y el próximo arranque la reprograma). Una que ya lo tomó se completa, y el
        // cierre la espera junto con su comprobante.
        await _candado.TomarAsync(_apagado);
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
                confirmada = reserva.TomarLectura();
                precio = modelo.Datos.Precio;
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

        if (confirmada is not null)
        {
            await GenerarComprobanteAsync(confirmada, precio);
        }
    }

    // Se genera después de confirmar en la base y fuera del candado. Si falla, la confirmación no
    // se revierte: se registra en consola (supuesto declarado en la documentación).
    private async Task GenerarComprobanteAsync(LecturaDeReserva reserva, decimal precio)
    {
        try
        {
            await _comprobantes.GenerarAsync(reserva, precio);
        }
        catch (Exception excepcion)
        {
            Console.Error.WriteLine(
                $"No se pudo generar el comprobante de la reserva {reserva.Id}: {excepcion.Message}");
        }
    }
}
