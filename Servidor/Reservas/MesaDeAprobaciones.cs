using Dominio.Fachada;

namespace Servidor.Reservas;

internal sealed class MesaDeAprobaciones
{
    private static readonly TimeSpan TimeoutDeCierre = TimeSpan.FromSeconds(5);

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

    public void Detener()
    {
        lock (_candado)
        {
            if (_detenida)
                return;

            _detenida = true;
            Monitor.PulseAll(_candado);
        }

        _thread?.Join(TimeoutDeCierre);
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
                // Monitor.Wait en vez de Sleep para que Detener() pueda despertar el ciclo de inmediato.
                Monitor.Wait(_candado, _intervalo);

            return !_detenida;
        }
    }

    private void ResolverSinPropagar()
    {
        try
        {
            _resolucionDeReservas.ResolverVencidas();
        }
        // Sin este catch una excepción mataría el Thread en silencio y no se resolverían más reservas.
        catch (Exception exception)
        {
            Console.Error.WriteLine($"La mesa de aprobaciones falló al resolver reservas: {exception}");
        }
    }
}
