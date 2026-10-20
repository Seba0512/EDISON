using System.Collections.Concurrent;

namespace Servidor.Dominio.Servicios;

// Fabricación pura: lleva la cuenta de las esperas de las reservas pendientes. Cada reserva tiene
// su propio CancellationTokenSource, vinculado al token de apagado del servidor, así una espera
// se corta por dos causas distintas: el usuario cancela la reserva o el servidor se apaga.
public sealed class ResolutorDeReservas
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _pendientes = new();
    // Esperas lanzadas que todavía no terminaron. Se usa como conjunto: el valor no importa.
    private readonly ConcurrentDictionary<Task, byte> _enCurso = new();
    private readonly Func<Guid, Task> _resolver;
    private readonly CancellationToken _apagado;

    public ResolutorDeReservas(Func<Guid, Task> resolver, CancellationToken apagado)
    {
        _resolver = resolver;
        _apagado = apagado;
    }

    // Lanza la espera y devuelve enseguida. La demora es cero para una reserva que ya venció
    // (por ejemplo tras un reinicio del servidor).
    public void Programar(Guid idReserva, TimeSpan demora)
    {
        CancellationTokenSource cancelacion = CancellationTokenSource.CreateLinkedTokenSource(_apagado);
        _pendientes[idReserva] = cancelacion;
        // ResolverTrasDemoraAsync registra sus propios errores: el Task solo se sigue para el cierre.
        Seguir(ResolverTrasDemoraAsync(idReserva, demora, cancelacion));
    }

    // Al apagar, las esperas se cortan solas con el token de apagado. Las que ya estaban resolviendo
    // no lo miran: terminan de escribir en la base y de generar el comprobante, y eso se espera acá.
    public Task EsperarResolucionesEnCursoAsync()
    {
        return Task.WhenAll(_enCurso.Keys);
    }

    // Cancelación del usuario: corta la espera si la reserva todavía estaba esperando.
    public void Cancelar(Guid idReserva)
    {
        if (_pendientes.TryRemove(idReserva, out CancellationTokenSource? cancelacion))
        {
            cancelacion.Cancel();
            cancelacion.Dispose();
        }
    }

    // La continuación corre aunque la espera ya haya terminado al registrarla, así que ninguna
    // espera terminada queda en el conjunto.
    private void Seguir(Task espera)
    {
        _enCurso.TryAdd(espera, 0);
        _ = espera.ContinueWith(
            terminada => _enCurso.TryRemove(terminada, out _), TaskScheduler.Default);
    }

    private async Task ResolverTrasDemoraAsync(
        Guid idReserva, TimeSpan demora, CancellationTokenSource cancelacion)
    {
        try
        {
            await Task.Delay(demora, cancelacion.Token);
            await _resolver(idReserva);
        }
        catch (OperationCanceledException) when (_apagado.IsCancellationRequested)
        {
            // Apagado del servidor: la reserva no se toca. Queda Pendiente en la base y el próximo
            // arranque la reprograma con la demora que le quede.
            Console.WriteLine($"Servidor en cierre: la reserva {idReserva} queda pendiente.");
        }
        catch (OperationCanceledException)
        {
            // Cancelación del usuario: es el final esperado. El estado Cancelada lo persiste
            // ServicioDeReservas.
            Console.WriteLine($"La espera de la reserva {idReserva} fue cancelada.");
        }
        catch (Exception excepcion)
        {
            // Sin este catch la excepción se perdería en silencio y la reserva quedaría pendiente.
            Console.Error.WriteLine($"No se pudo resolver la reserva {idReserva}: {excepcion}");
        }
        finally
        {
            // Por cualquier camino el CTS sale del diccionario, para que las reservas resueltas
            // normalmente no queden en memoria. Si Cancelar ya lo sacó, no hay nada que hacer.
            if (_pendientes.TryRemove(idReserva, out CancellationTokenSource? pendiente))
            {
                pendiente.Dispose();
            }
        }
    }
}
