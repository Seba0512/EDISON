namespace Servidor.Dominio.Servicios;

// Candado único y global, como recomienda el curso: quien opera lo toma una vez y adentro hace
// leer-validar-modificar de forma atómica. Es un SemaphoreSlim porque un lock no puede contener
// await. Supuesto documentado: hay una sola instancia del servidor, así que este candado en
// memoria alcanza para serializar las escrituras sobre la base.
public sealed class CandadoGlobal
{
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    // Con token, la espera se puede abandonar: si se cancela antes de obtener el candado, lanza
    // OperationCanceledException y el candado no queda tomado.
    public Task TomarAsync(CancellationToken token = default) => _semaforo.WaitAsync(token);

    public void Liberar() => _semaforo.Release();
}
