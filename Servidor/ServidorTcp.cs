using System.Net.Sockets;
using Common.Protocolo;
using Servidor.Comandos;
using Servidor.Composicion;
using Servidor.Red;
using Servidor.Reservas;

namespace Servidor;

internal sealed class ServidorTcp : IDisposable
{
    private const int Ejecutando = 1;
    private const int Detenido = 0;

    private readonly ConfiguracionServidor _configuracion;
    private readonly object _candadoCicloDeVida = new();
    private readonly RegistroDeConexiones _registro = new();
    private readonly CodecDeTramas _codec;
    private readonly CanalDeArchivos _canalDeArchivos;
    private readonly DespachadorDeComandos _despachador;
    private readonly MesaDeAprobaciones _mesaDeAprobaciones;
    private Socket? _listener;
    private Thread? _threadAceptador;
    private int _ejecutando;
    private int _siguienteCliente;

    public ServidorTcp(ConfiguracionServidor configuracion)
    {
        _configuracion = configuracion;
        _codec = new CodecDeTramas(configuracion.MaxPayloadBytes);
        _canalDeArchivos = new CanalDeArchivos(configuracion.FileBlockBytes);

        NucleoDelServidor nucleo = NucleoDelServidor.Crear(configuracion);
        _despachador = nucleo.Despachador;
        _mesaDeAprobaciones = nucleo.MesaDeAprobaciones;
    }

    public void Iniciar()
    {
        bool inicioAdquirido = false;
        try
        {
            lock (_candadoCicloDeVida)
            {
                ExigirServidorDetenido();
                inicioAdquirido = true;
                IniciarRecursos();
            }
        }
        catch when (inicioAdquirido)
        {
            Interlocked.Exchange(ref _ejecutando, Detenido);
            if (!CerrarRecursos())
                Console.Error.WriteLine("El rollback del arranque dejó recursos pendientes");
            throw;
        }
    }

    public bool Detener()
    {
        lock (_candadoCicloDeVida)
        {
            if (!HayRecursosParaCerrar())
                return true;
        }

        Console.WriteLine("Cerrando servidor...");
        bool cierreCompleto = CerrarRecursos();
        if (cierreCompleto)
            Console.WriteLine("Servidor cerrado ordenadamente.");
        return cierreCompleto;
    }

    public void Dispose()
        => Detener();

    private void ExigirServidorDetenido()
    {
        if (Volatile.Read(ref _ejecutando) == Ejecutando || HayRecursosPoseidos())
            throw new InvalidOperationException("El servidor ya fue iniciado o tiene recursos pendientes");

        Interlocked.Exchange(ref _ejecutando, Ejecutando);
    }

    private void IniciarRecursos()
    {
        _listener = CrearListener();
        _mesaDeAprobaciones.Iniciar();
        _threadAceptador = new Thread(AceptarConexiones) { Name = "Aceptador TCP" };
        _registro.AbrirAdmision();
        _threadAceptador.Start();
        Console.WriteLine($"Servidor escuchando en {_configuracion.Endpoint}");
    }

    private Socket CrearListener()
    {
        Socket listener = new(
            _configuracion.ListenIp.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);
        try
        {
            listener.Bind(_configuracion.Endpoint);
            listener.Listen(_configuracion.Backlog);
            return listener;
        }
        catch
        {
            listener.Dispose();
            throw;
        }
    }

    private void AceptarConexiones()
    {
        while (EstaEjecutando())
        {
            try
            {
                Socket socket = ObtenerListener().Accept();
                IncorporarCliente(socket);
            }
            catch (ObjectDisposedException) when (EstaDetenido())
            {
                // Detener() cerró el listener. Este es el final esperado del bucle, no un error.
                return;
            }
            catch (SocketException) when (EstaDetenido())
            {
                return;
            }
            catch (SocketException exception)
            {
                Console.Error.WriteLine($"Accept falló: {exception.Message}");
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"No se pudo iniciar una sesión: {exception}");
            }
        }
    }

    private void IncorporarCliente(Socket socket)
    {
        int numero = Interlocked.Increment(ref _siguienteCliente);
        ConexionActiva conexion = new(numero, socket);
        Thread thread = new(() => Atender(conexion))
        {
            Name = $"Cliente {numero}"
        };
        conexion.AsignarThread(thread);
        IncorporarODescartar(conexion);
    }

    private void IncorporarODescartar(ConexionActiva conexion)
    {
        try
        {
            if (!_registro.IntentarAgregarEIniciar(conexion))
                conexion.Cerrar();
        }
        catch
        {
            conexion.Cerrar();
            throw;
        }
    }

    private void Atender(ConexionActiva conexion)
    {
        try
        {
            new SesionDeCliente(
                    conexion,
                    _codec,
                    _despachador,
                    new EstadoDeSesion(),
                    _canalDeArchivos,
                    _configuracion.MaxFileBytes)
                .Atender();
        }
        finally
        {
            _registro.Quitar(conexion);
        }
    }

    private bool EstaEjecutando()
        => Volatile.Read(ref _ejecutando) == Ejecutando;

    private bool EstaDetenido()
        => Volatile.Read(ref _ejecutando) == Detenido;

    private Socket ObtenerListener()
        => _listener ?? throw new ObjectDisposedException(nameof(ServidorTcp));

    private bool CerrarRecursos()
    {
        List<string> problemas = [];
        ConexionActiva[] conexiones = PrepararCierre();
        IntentarCerrarListener(problemas);
        IntentarUnirAceptador(problemas);
        IntentarDetenerMesa(problemas);
        IntentarCerrarConexiones(conexiones, problemas);
        IntentarUnirConexiones(conexiones, problemas);
        _registro.DescartarTerminadas();
        VerificarRecursosVivos(problemas);
        return InformarResultado(problemas);
    }

    private ConexionActiva[] PrepararCierre()
    {
        lock (_candadoCicloDeVida)
        {
            Interlocked.Exchange(ref _ejecutando, Detenido);
            return _registro.CerrarAdmisionYTomarSnapshot();
        }
    }

    private void IntentarCerrarListener(List<string> problemas)
    {
        Socket? listener = Interlocked.Exchange(ref _listener, null);
        if (listener is null)
            return;

        try
        {
            listener.Dispose();
        }
        catch (Exception exception)
        {
            Interlocked.CompareExchange(ref _listener, listener, null);
            problemas.Add(DescribirProblema("No se pudo cerrar el listener", exception));
        }
    }

    private void IntentarUnirAceptador(List<string> problemas)
    {
        Thread? thread = Volatile.Read(ref _threadAceptador);
        if (thread is null)
            return;

        try
        {
            if (EsThreadSinIniciar(thread) || thread.Join(_configuracion.JoinTimeoutMilliseconds))
                Interlocked.CompareExchange(ref _threadAceptador, null, thread);
            else
                problemas.Add("El Thread aceptador no terminó dentro del timeout");
        }
        catch (Exception exception)
        {
            problemas.Add(DescribirProblema("No se pudo esperar al Thread aceptador", exception));
        }
    }

    private void IntentarDetenerMesa(List<string> problemas)
    {
        try
        {
            TimeSpan timeout = TimeSpan.FromMilliseconds(_configuracion.JoinTimeoutMilliseconds);
            if (!_mesaDeAprobaciones.Detener(timeout))
                problemas.Add("La mesa de aprobaciones no terminó dentro del timeout");
        }
        catch (Exception exception)
        {
            problemas.Add(DescribirProblema("No se pudo detener la mesa de aprobaciones", exception));
        }
    }

    private static void IntentarCerrarConexiones(
        IEnumerable<ConexionActiva> conexiones,
        List<string> problemas)
    {
        foreach (ConexionActiva conexion in conexiones)
        {
            try
            {
                conexion.Cerrar();
            }
            catch (Exception exception)
            {
                problemas.Add(DescribirProblema(
                    $"No se pudo cerrar el cliente {conexion.Numero}",
                    exception));
            }
        }
    }

    private void IntentarUnirConexiones(
        IEnumerable<ConexionActiva> conexiones,
        List<string> problemas)
    {
        foreach (ConexionActiva conexion in conexiones)
            IntentarUnirConexion(conexion, problemas);
    }

    private void IntentarUnirConexion(ConexionActiva conexion, List<string> problemas)
    {
        try
        {
            TimeSpan timeout = TimeSpan.FromMilliseconds(_configuracion.JoinTimeoutMilliseconds);
            if (!conexion.Unir(timeout))
                problemas.Add($"El Thread del cliente {conexion.Numero} no terminó dentro del timeout");
        }
        catch (Exception exception)
        {
            problemas.Add(DescribirProblema(
                $"No se pudo esperar al cliente {conexion.Numero}",
                exception));
        }
    }

    private void VerificarRecursosVivos(List<string> problemas)
    {
        if (Volatile.Read(ref _listener) is not null)
            problemas.Add("El listener continúa asignado");
        if (Volatile.Read(ref _threadAceptador)?.IsAlive == true)
            problemas.Add("El Thread aceptador continúa vivo");
        if (_mesaDeAprobaciones.TieneThreadVivo)
            problemas.Add("El Thread de la mesa de aprobaciones continúa vivo");

        foreach (ConexionActiva conexion in _registro.TomarSnapshot())
            if (conexion.TieneThreadVivo)
                problemas.Add($"El Thread del cliente {conexion.Numero} continúa vivo");
    }

    private bool HayRecursosParaCerrar()
        => Volatile.Read(ref _ejecutando) == Ejecutando
           || HayRecursosPoseidos();

    private bool HayRecursosPoseidos()
        => Volatile.Read(ref _listener) is not null
           || Volatile.Read(ref _threadAceptador) is not null
           || _mesaDeAprobaciones.TieneThreadAsignado
           || _registro.TieneConexiones;

    private static bool InformarResultado(List<string> problemas)
    {
        string[] unicos = problemas.Distinct().ToArray();
        foreach (string problema in unicos)
            Console.Error.WriteLine($"Cierre pendiente: {problema}");

        if (unicos.Length > 0)
        {
            Console.Error.WriteLine("El servidor no terminó de cerrar todos sus recursos.");
            return false;
        }

        return true;
    }

    private static string DescribirProblema(string descripcion, Exception exception)
        => $"{descripcion}: {exception.GetType().Name}: {exception.Message}";

    private static bool EsThreadSinIniciar(Thread thread)
        => (thread.ThreadState & ThreadState.Unstarted) != 0;
}
