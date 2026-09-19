using Dominio;

namespace Servidor;

// Cada tanto revisa las reservas pendientes cuyo plazo venció y las confirma o rechaza.
internal sealed class MesaDeAprobaciones
{
    private readonly Edison _edison;
    private readonly TimeSpan _intervalo;
    private readonly object _candado = new();
    private Thread? _thread;
    private bool _detenida;

    public MesaDeAprobaciones(Edison edison, TimeSpan intervalo)
    {
        _edison = edison;
        _intervalo = intervalo;
    }

    public bool TieneThreadAsignado
    {
        get
        {
            lock (_candado)
            {
                return _thread is not null;
            }
        }
    }

    public bool TieneThreadVivo
    {
        get
        {
            lock (_candado)
            {
                return _thread?.IsAlive == true;
            }
        }
    }

    public void Iniciar()
    {
        lock (_candado)
        {
            if (_thread is not null || _detenida)
            {
                throw new InvalidOperationException("La mesa de aprobaciones ya fue iniciada");
            }

            _thread = new Thread(Ciclo) { Name = "Mesa de aprobaciones" };
            IniciarThread();
        }
    }

    public bool Detener(TimeSpan timeout)
    {
        Thread? thread;
        lock (_candado)
        {
            _detenida = true;
            Monitor.PulseAll(_candado);
            thread = _thread;
        }

        if (thread is null || EsThreadSinIniciar(thread))
        {
            LiberarThread(thread);
            return true;
        }

        bool termino = thread.Join(timeout);
        if (termino)
        {
            LiberarThread(thread);
        }

        return termino;
    }

    private void Ciclo()
    {
        while (EsperarIntervalo())
        {
            ResolverSinPropagar();
        }
    }

    private bool EsperarIntervalo()
    {
        lock (_candado)
        {
            if (!_detenida)
            {
                // Monitor.Wait en vez de Sleep para que Detener() pueda despertar el ciclo de inmediato.
                Monitor.Wait(_candado, _intervalo);
            }

            return !_detenida;
        }
    }

    private void ResolverSinPropagar()
    {
        try
        {
            _edison.ResolverVencidas();
        }
        catch (Exception exception)
        {
            // Sin este catch una excepción mataría el Thread en silencio y no se resolverían más reservas.
            Console.Error.WriteLine($"La mesa de aprobaciones falló al resolver reservas: {exception}");
        }
    }

    private void IniciarThread()
    {
        try
        {
            _thread!.Start();
        }
        catch
        {
            _thread = null;
            throw;
        }
    }

    private void LiberarThread(Thread? thread)
    {
        lock (_candado)
        {
            if (ReferenceEquals(_thread, thread))
            {
                _thread = null;
            }
        }
    }

    private static bool EsThreadSinIniciar(Thread thread)
    {
        return (thread.ThreadState & ThreadState.Unstarted) != 0;
    }
}
