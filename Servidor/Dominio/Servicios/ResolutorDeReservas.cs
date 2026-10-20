using System.Collections.Concurrent;

namespace Servidor.Dominio.Servicios;

// Fabricación pura: lleva la cuenta de las esperas de las reservas pendientes. Cada reserva tiene
// su propio CancellationTokenSource, vinculado al token de apagado del servidor, así una espera
// se corta por dos causas distintas: el usuario cancela la reserva o el servidor se apaga.
public sealed class ResolutorDeReservas
{
    private const int IntentosDeResolucion = 5;
    private static readonly TimeSpan PausaEntreIntentos = TimeSpan.FromSeconds(10);

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
        Programar(idReserva, demora, intento: 1);
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

    private void Programar(Guid idReserva, TimeSpan demora, int intento)
    {
        CancellationTokenSource cancelacion = CancellationTokenSource.CreateLinkedTokenSource(_apagado);
        _pendientes[idReserva] = cancelacion;
        // ResolverTrasDemoraAsync registra sus propios errores: el Task solo se sigue para el cierre.
        Seguir(ResolverTrasDemoraAsync(new EsperaDeReserva(idReserva, intento, cancelacion), demora));
    }

    private async Task ResolverTrasDemoraAsync(EsperaDeReserva espera, TimeSpan demora)
    {
        bool reintentar = false;
        try
        {
            await Task.Delay(demora, espera.Cancelacion.Token);
            await _resolver(espera.IdReserva);
        }
        catch (OperationCanceledException) when (_apagado.IsCancellationRequested)
        {
            // Apagado del servidor: la reserva no se toca. Queda Pendiente en la base y el próximo
            // arranque la reprograma con la demora que le quede.
            Console.WriteLine($"Servidor en cierre: la reserva {espera.IdReserva} queda pendiente.");
        }
        catch (OperationCanceledException)
        {
            // Cancelación del usuario: es el final esperado. El estado Cancelada lo persiste
            // ServicioDeReservas.
            Console.WriteLine($"La espera de la reserva {espera.IdReserva} fue cancelada.");
        }
        catch (Exception excepcion)
        {
            // Sin este catch la excepción se perdería en silencio y la reserva quedaría pendiente.
            reintentar = espera.Intento < IntentosDeResolucion && !_apagado.IsCancellationRequested;
            InformarFalla(espera, excepcion, reintentar);
        }
        finally
        {
            // Por cualquier camino el CTS sale del diccionario, para que las reservas resueltas
            // normalmente no queden en memoria. Solo se saca el propio: si Cancelar ya lo sacó y lo
            // dispuso, no hay nada que hacer.
            if (_pendientes.TryRemove(KeyValuePair.Create(espera.IdReserva, espera.Cancelacion)))
            {
                espera.Cancelacion.Dispose();
            }
        }

        // Una falla pasajera (por ejemplo, MySQL caído justo al vencer el plazo) no deja la reserva
        // pendiente hasta el próximo arranque. Si el usuario la cancela mientras tanto, la
        // resolución la encuentra Cancelada en la base y no hace nada.
        if (reintentar)
        {
            Programar(espera.IdReserva, PausaEntreIntentos, espera.Intento + 1);
        }
    }

    private static void InformarFalla(EsperaDeReserva espera, Exception excepcion, bool reintentar)
    {
        string desenlace = reintentar
            ? $"se reintenta en {PausaEntreIntentos.TotalSeconds:0} s"
            : "queda pendiente hasta el próximo arranque del servidor";
        Console.Error.WriteLine(
            $"No se pudo resolver la reserva {espera.IdReserva} " +
            $"(intento {espera.Intento} de {IntentosDeResolucion}), {desenlace}: {excepcion.Message}");
    }

    private readonly record struct EsperaDeReserva(
        Guid IdReserva, int Intento, CancellationTokenSource Cancelacion);
}
