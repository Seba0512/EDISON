using System.Net.Sockets;
using Common;

namespace Servidor;

// El modelo del curso: un flag de ejecución, un Thread aceptador, un Thread por cliente y una
// lista de conexiones con lock. Detener cierra el listener y los sockets activos, y eso
// desbloquea los Accept y Receive pendientes para poder esperar cada Thread.
internal sealed class ServidorTcp : IDisposable
{
    // Conexiones que el sistema operativo encola mientras el aceptador no las tomó con Accept.
    // No limita los clientes conectados, y como el aceptador las toma enseguida, alcanza con pocas.
    private const int ConexionesEnEspera = 10;

    // Al cerrar se espera a cada Thread como mucho este tiempo: si alguno no termina, se informa
    // en vez de dejar el cierre colgado.
    private static readonly TimeSpan EsperaMaximaPorThread = TimeSpan.FromSeconds(5);

    private readonly ConfiguracionServidor _configuracion;
    private readonly EjecutorDeComandos _ejecutor;
    private readonly CodecDeTramas _codec;
    private readonly List<ConexionDeCliente> _conexiones = new();
    private readonly object _candado = new();
    private volatile bool _ejecutando;
    private Socket? _listener;
    private Thread? _aceptador;
    private int _siguienteCliente;

    public ServidorTcp(ConfiguracionServidor configuracion, EjecutorDeComandos ejecutor)
    {
        _configuracion = configuracion;
        _ejecutor = ejecutor;
        _codec = new CodecDeTramas();
    }

    public void Iniciar()
    {
        if (_listener is not null)
        {
            throw new InvalidOperationException("El servidor ya fue iniciado");
        }

        Socket listener = new(
            _configuracion.ListenIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            listener.Bind(_configuracion.Endpoint);
            listener.Listen(ConexionesEnEspera);
        }
        catch
        {
            listener.Dispose();
            throw;
        }

        _listener = listener;
        _ejecutando = true;
        _aceptador = new Thread(AceptarConexiones) { Name = "Aceptador TCP" };
        _aceptador.Start();
        Console.WriteLine($"Servidor escuchando en {_configuracion.Endpoint}");
    }

    public bool Detener()
    {
        if (_listener is null)
        {
            return true;
        }

        Console.WriteLine("Cerrando servidor...");
        _ejecutando = false;
        _listener?.Close();

        bool cierreCompleto = UnirThread(_aceptador!, "aceptador");
        ConexionDeCliente[] activas;
        lock (_candado)
        {
            activas = _conexiones.ToArray();
        }

        foreach (ConexionDeCliente conexion in activas)
        {
            conexion.Cerrar();
        }

        foreach (ConexionDeCliente conexion in activas)
        {
            cierreCompleto &= UnirThread(conexion.Thread, $"del cliente {conexion.Numero}");
        }

        if (cierreCompleto)
        {
            Console.WriteLine("Servidor cerrado ordenadamente.");
        }
        else
        {
            Console.Error.WriteLine("El servidor no terminó de cerrar todos sus recursos.");
        }

        return cierreCompleto;
    }

    public void Dispose()
    {
        Detener();
    }

    private void AceptarConexiones()
    {
        while (_ejecutando)
        {
            try
            {
                Socket socket = _listener!.Accept();
                IncorporarCliente(socket);
            }
            catch (SocketException) when (!_ejecutando)
            {
                // Detener() cerró el listener. Este es el final esperado del bucle, no un error.
                return;
            }
            catch (ObjectDisposedException) when (!_ejecutando)
            {
                return;
            }
            catch (SocketException exception)
            {
                Console.Error.WriteLine($"Accept falló: {exception.Message}");
            }
        }
    }

    private void IncorporarCliente(Socket socket)
    {
        int numero = ++_siguienteCliente;
        // La sesión abre su stream acá y no en el Thread del cliente: Detener cierra los sockets
        // recién después de esperar al aceptador, así que este todavía no pudo cerrarse.
        SesionDeCliente sesion = new(socket, numero, _codec, _ejecutor);
        ConexionDeCliente? conexion = null;
        Thread thread = new(() => Atender(conexion!, sesion)) { Name = $"Cliente {numero}" };
        conexion = new ConexionDeCliente(numero, socket, thread);
        lock (_candado)
        {
            // Si el cierre ya empezó, esta conexión no entra: se despide acá y no arranca su Thread.
            if (!_ejecutando)
            {
                conexion.Cerrar();
                return;
            }

            _conexiones.Add(conexion);
        }

        thread.Start();
    }

    private void Atender(ConexionDeCliente conexion, SesionDeCliente sesion)
    {
        try
        {
            sesion.Atender();
        }
        finally
        {
            conexion.Cerrar();
            lock (_candado)
            {
                _conexiones.Remove(conexion);
            }
        }
    }

    private bool UnirThread(Thread thread, string descripcion)
    {
        if (thread.Join(EsperaMaximaPorThread))
        {
            return true;
        }

        Console.Error.WriteLine($"El Thread {descripcion} no terminó dentro del timeout");
        return false;
    }

    private sealed record ConexionDeCliente(int Numero, Socket Socket, Thread Thread)
    {
        // Cerrar dos veces no es un error: el socket ya cerrado simplemente se ignora.
        public void Cerrar()
        {
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            Socket.Close();
        }
    }
}
