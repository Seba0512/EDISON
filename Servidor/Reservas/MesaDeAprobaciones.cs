using Dominio.Fachada;

namespace Servidor.Reservas;

/// <summary>
/// Resolución periódica de reservas vencidas (CR5): un <see cref="Thread"/> propio despierta cada
/// <c>intervalo</c> y llama a <see cref="IResolucionDeReservas.ResolverVencidas"/>. Usa
/// <see cref="Monitor.Wait(object, TimeSpan)"/>/<see cref="Monitor.PulseAll"/> en vez de
/// <see cref="Thread.Sleep(TimeSpan)"/> para que <see cref="Detener"/> despierte el ciclo de
/// inmediato en lugar de esperar hasta el próximo intervalo.
///
/// No es background a propósito: SR4 exige un cierre ordenado, y un Thread background podría
/// cortarse a mitad de una resolución si el proceso terminara antes de que <see cref="Detener"/> lo
/// una.
/// </summary>
internal sealed class MesaDeAprobaciones
{
    private readonly IResolucionDeReservas _resolucionDeReservas;
    private readonly TimeSpan _intervalo;
    private readonly object _candado = new();
    private Thread? _thread;
    private bool _detenida;

    public MesaDeAprobaciones(IResolucionDeReservas resolucionDeReservas, TimeSpan intervalo)
    {
        _resolucionDeReservas = resolucionDeReservas;
        _intervalo = intervalo;
    }

    public void Iniciar()
    {
        _thread = new Thread(Ciclo) { Name = "Mesa de aprobaciones" };
        _thread.Start();
    }

    /// <summary>Idempotente: una segunda llamada no hace nada.</summary>
    public void Detener()
    {
        lock (_candado)
        {
            if (_detenida)
                return;

            _detenida = true;
            Monitor.PulseAll(_candado);
        }

        _thread?.Join(TimeSpan.FromSeconds(5));
    }

    private void Ciclo()
    {
        while (EsperarIntervalo())
            ResolverSinPropagar();
    }

    private bool EsperarIntervalo()
    {
        lock (_candado)
        {
            if (!_detenida)
                Monitor.Wait(_candado, _intervalo);

            return !_detenida;
        }
    }

    /// <summary>
    /// Se llama fuera del candado del ciclo: <see cref="IResolucionDeReservas.ResolverVencidas"/> ya
    /// toma sus propios candados de dominio y no tiene por qué competir con <see cref="Detener"/>.
    /// Envuelta en try/catch porque una excepción sin atrapar mataría este Thread en silencio y CR5
    /// dejaría de resolverse reservas para siempre.
    /// </summary>
    private void ResolverSinPropagar()
    {
        try
        {
            _resolucionDeReservas.ResolverVencidas();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"La mesa de aprobaciones falló al resolver reservas: {exception}");
        }
    }
}
