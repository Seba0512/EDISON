using System.Configuration;
using System.Net.Sockets;
using Common;
using Server.Commands;
using Server.Connections;
using Server.Domain.Facade;
using Server.Domain.Repositories;
using Server.Domain.Services;

namespace Server;

// Raíz de composición: crea repositorios, fachada y handlers, y acepta clientes. Cada cliente
// aceptado se atiende en su propio Thread (ver Connections/SesionDeCliente).
internal sealed class Server : IDisposable
{
    private const string OrdenDeSalida = "salir";
    private const int Ejecutando = 1;
    private const int Detenido = 0;

    private readonly ConfiguracionServidor _configuracion;
    private readonly object _candadoCicloDeVida = new();
    private readonly RegistroDeConexiones _registro = new();
    private readonly RecursosDeSesion _recursos;
    private readonly MesaDeAprobaciones _mesaDeAprobaciones;
    private Socket? _listener;
    private Thread? _threadAceptador;
    private int _ejecutando;
    private int _siguienteCliente;

    private Server(
        ConfiguracionServidor configuracion,
        DespachadorDeComandos despachador,
        MesaDeAprobaciones mesaDeAprobaciones)
    {
        _configuracion = configuracion;
        _recursos = new RecursosDeSesion(
            new CodecDeTramas(configuracion.MaxPayloadBytes),
            new CanalDeArchivos(configuracion.FileBlockBytes),
            despachador,
            configuracion.MaxFileBytes);
        _mesaDeAprobaciones = mesaDeAprobaciones;
    }

    private TimeSpan TimeoutDeCierre
        => TimeSpan.FromMilliseconds(_configuracion.JoinTimeoutMilliseconds);

    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        try
        {
            Ejecutar();
            return 0;
        }
        catch (ConfigurationErrorsException exception)
        {
            Console.Error.WriteLine($"Configuración inválida: {exception.Message}");
            return 1;
        }
        catch (SocketException exception)
        {
            Console.Error.WriteLine($"No se pudo iniciar el servidor: {exception.Message}");
            return 1;
        }
    }

    private static void Ejecutar()
    {
        ConfiguracionServidor configuracion = ConfiguracionServidor.Cargar();
        Edison edison = CrearFachada(configuracion);
        ImagenesEnDisco imagenes = new(configuracion.ImagesDirectory, configuracion.MaxFileBytes);
        MesaDeAprobaciones mesa = new(
            edison, TimeSpan.FromMilliseconds(configuracion.ApprovalIntervalMilliseconds));
        DespachadorDeComandos despachador = CatalogoDeComandos.Crear(edison, imagenes);
        using Server servidor = new(configuracion, despachador, mesa);
        servidor.Iniciar();
        EsperarOrdenDeSalida();
        servidor.Detener();
    }

    private static Edison CrearFachada(ConfiguracionServidor configuracion)
    {
        RepositoriosDelDominio repositorios = new(
            new RepositorioDeUsuariosEnMemoria(),
            new RepositorioDeModelosEnMemoria(),
            new RepositorioDeReservasEnMemoria());

        return new Edison(
            repositorios, new RelojDelSistema(), configuracion.ReservationDelaySeconds);
    }

    private static void EsperarOrdenDeSalida()
    {
        Console.WriteLine($"Escriba '{OrdenDeSalida}' para cerrar el servidor.");
        while (true)
        {
            string? orden = Console.ReadLine();
            // Fin de entrada (por ejemplo con la entrada redirigida) cierra igual que la orden.
            if (orden is null || EsOrdenDeSalida(orden))
                return;

            Console.WriteLine($"Orden desconocida. Escriba '{OrdenDeSalida}'.");
        }
    }

    private static bool EsOrdenDeSalida(string orden)
        => orden.Equals(OrdenDeSalida, StringComparison.OrdinalIgnoreCase);

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
            {
                Console.Error.WriteLine("El rollback del arranque dejó recursos pendientes");
            }

            throw;
        }
    }

    public bool Detener()
    {
        lock (_candadoCicloDeVida)
        {
            if (!HayRecursosParaCerrar())
            {
                return true;
            }
        }

        Console.WriteLine("Cerrando servidor...");
        bool cierreCompleto = CerrarRecursos();
        if (cierreCompleto)
        {
            Console.WriteLine("Servidor cerrado ordenadamente.");
        }

        return cierreCompleto;
    }

    public void Dispose()
    {
        Detener();
    }

    private void ExigirServidorDetenido()
    {
        if (Volatile.Read(ref _ejecutando) == Ejecutando || HayRecursosPoseidos())
        {
            throw new InvalidOperationException("El servidor ya fue iniciado o tiene recursos pendientes");
        }

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
            {
                conexion.Cerrar();
            }
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
            new SesionDeCliente(conexion, _recursos).Atender();
        }
        finally
        {
            _registro.Quitar(conexion);
        }
    }

    private bool EstaEjecutando()
    {
        return Volatile.Read(ref _ejecutando) == Ejecutando;
    }

    private bool EstaDetenido()
    {
        return Volatile.Read(ref _ejecutando) == Detenido;
    }

    private Socket ObtenerListener()
    {
        return _listener ?? throw new ObjectDisposedException(nameof(Server));
    }

    private bool CerrarRecursos()
    {
        List<string> problemas = [];
        PrepararCierre();
        IntentarCerrarListener(problemas);
        IntentarUnirAceptador(problemas);
        IntentarDetenerMesa(problemas);
        problemas.AddRange(_registro.CerrarTodas(TimeoutDeCierre));
        VerificarRecursosVivos(problemas);
        return InformarResultado(problemas);
    }

    private void PrepararCierre()
    {
        lock (_candadoCicloDeVida)
        {
            Interlocked.Exchange(ref _ejecutando, Detenido);
            _registro.CerrarAdmision();
        }
    }

    private void IntentarCerrarListener(List<string> problemas)
    {
        Socket? listener = Interlocked.Exchange(ref _listener, null);
        if (listener is null)
        {
            return;
        }

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
        {
            return;
        }

        try
        {
            if (EsThreadSinIniciar(thread) || thread.Join(TimeoutDeCierre))
            {
                Interlocked.CompareExchange(ref _threadAceptador, null, thread);
            }
            else
            {
                problemas.Add("El Thread aceptador no terminó dentro del timeout");
            }
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
            if (!_mesaDeAprobaciones.Detener(TimeoutDeCierre))
            {
                problemas.Add("La mesa de aprobaciones no terminó dentro del timeout");
            }
        }
        catch (Exception exception)
        {
            problemas.Add(DescribirProblema("No se pudo detener la mesa de aprobaciones", exception));
        }
    }

    private void VerificarRecursosVivos(List<string> problemas)
    {
        if (Volatile.Read(ref _listener) is not null)
        {
            problemas.Add("El listener continúa asignado");
        }

        if (Volatile.Read(ref _threadAceptador)?.IsAlive == true)
        {
            problemas.Add("El Thread aceptador continúa vivo");
        }

        if (_mesaDeAprobaciones.TieneThreadVivo)
        {
            problemas.Add("El Thread de la mesa de aprobaciones continúa vivo");
        }
    }

    private bool HayRecursosParaCerrar()
    {
        return Volatile.Read(ref _ejecutando) == Ejecutando || HayRecursosPoseidos();
    }

    private bool HayRecursosPoseidos()
    {
        return Volatile.Read(ref _listener) is not null
            || Volatile.Read(ref _threadAceptador) is not null
            || _mesaDeAprobaciones.TieneThreadAsignado
            || _registro.TieneConexiones;
    }

    private static bool InformarResultado(List<string> problemas)
    {
        string[] unicos = problemas.Distinct().ToArray();
        foreach (string problema in unicos)
        {
            Console.Error.WriteLine($"Cierre pendiente: {problema}");
        }

        if (unicos.Length > 0)
        {
            Console.Error.WriteLine("El servidor no terminó de cerrar todos sus recursos.");
            return false;
        }

        return true;
    }

    private static string DescribirProblema(string descripcion, Exception exception)
    {
        return $"{descripcion}: {exception.GetType().Name}: {exception.Message}";
    }

    private static bool EsThreadSinIniciar(Thread thread)
    {
        return (thread.ThreadState & ThreadState.Unstarted) != 0;
    }
}
