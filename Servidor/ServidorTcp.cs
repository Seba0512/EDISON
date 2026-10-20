using System.Collections.Concurrent;
using System.Net.Sockets;
using Common;

namespace Servidor;

// Un Task aceptador y un Task por cliente. El cierre se coordina con un CancellationTokenSource:
// DetenerAsync lo cancela, cierra el listener y los sockets, y espera a los Tasks con un tope.
internal sealed class ServidorTcp : IAsyncDisposable
{
    // Conexiones que el sistema operativo encola mientras el aceptador no las tomó con Accept.
    // No limita los clientes conectados, y como el aceptador las toma enseguida, alcanza con pocas.
    private const int ConexionesEnEspera = 10;

    // Al cerrar se espera a los Tasks como mucho este tiempo: si alguno no termina, se informa
    // en vez de dejar el cierre colgado.
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PausaTrasFallaDeAccept = TimeSpan.FromMilliseconds(200);

    private readonly ConfiguracionServidor _configuracion;
    private readonly EjecutorDeComandos _ejecutor;
    private readonly CodecDeTramas _codec;
    private readonly ConcurrentDictionary<int, ConexionDeCliente> _conexiones = new();
    private readonly CancellationTokenSource _ejecucion = new();
    private Socket? _listener;
    private Task _aceptador = Task.CompletedTask;
    private int _siguienteCliente;
    private bool _detenido;

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
        // El método async corre hasta su primer await y devuelve el Task: no hace falta Task.Run.
        _aceptador = AceptarConexionesAsync(listener, _ejecucion.Token);
        Console.WriteLine($"Servidor escuchando en {_configuracion.Endpoint}");
    }

    public async Task<bool> DetenerAsync()
    {
        if (_listener is null || _detenido)
        {
            return true;
        }

        _detenido = true;
        Console.WriteLine("Cerrando servidor...");
        await _ejecucion.CancelAsync();
        _listener.Close();

        // Primero el aceptador: después de que termina no puede aparecer ninguna conexión nueva.
        bool cierreCompleto = await EsperarAsync(_aceptador, "aceptador");
        ConexionDeCliente[] activas = _conexiones.Values.ToArray();
        foreach (ConexionDeCliente conexion in activas)
        {
            conexion.Cerrar();
        }

        cierreCompleto &= await EsperarAsync(
            Task.WhenAll(activas.Select(conexion => conexion.Tarea)), "de los clientes");

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

    public async ValueTask DisposeAsync()
    {
        await DetenerAsync();
        _ejecucion.Dispose();
    }

    private async Task AceptarConexionesAsync(Socket listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                Socket socket = await listener.AcceptAsync(token);
                IncorporarCliente(socket, token);
            }
            catch (OperationCanceledException)
            {
                // DetenerAsync canceló el token. Este es el final esperado del bucle, no un error.
                return;
            }
            catch (ObjectDisposedException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (SocketException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (SocketException exception)
            {
                Console.Error.WriteLine($"Accept falló: {exception.Message}");
                await PausarTrasFallaAsync(token);
            }
        }
    }

    // Un error que se repite enseguida (por ejemplo, sin descriptores de archivo libres) no puede
    // dejar al aceptador girando y llenando la consola.
    private static async Task PausarTrasFallaAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(PausaTrasFallaDeAccept, token);
        }
        catch (OperationCanceledException)
        {
            // El cierre corta la pausa; el while del aceptador ve el token cancelado y termina.
        }
    }

    private void IncorporarCliente(Socket socket, CancellationToken token)
    {
        int numero = Interlocked.Increment(ref _siguienteCliente);
        // La sesión abre su stream acá y no en el Task del cliente: DetenerAsync cierra los
        // sockets recién después de esperar al aceptador, así que este todavía no pudo cerrarse.
        SesionDeCliente sesion;
        try
        {
            sesion = new SesionDeCliente(socket, numero, _codec, _ejecutor);
        }
        catch (IOException exception)
        {
            // El cliente se fue entre el Accept y la creación de su stream: se lo descarta y el
            // aceptador sigue atendiendo a los demás.
            Console.Error.WriteLine($"No se pudo atender al cliente {numero}: {exception.Message}");
            socket.Dispose();
            return;
        }

        ConexionDeCliente conexion = new(numero, socket);
        _conexiones[numero] = conexion;
        conexion.Tarea = AtenderAsync(conexion, sesion, token);
    }

    private async Task AtenderAsync(
        ConexionDeCliente conexion, SesionDeCliente sesion, CancellationToken token)
    {
        try
        {
            // Un método async corre en el hilo de quien lo llama hasta su primera espera real: sin
            // este Yield, un cliente que manda tramas sin pausa retendría al aceptador y los demás
            // clientes esperarían para conectarse.
            await Task.Yield();
            await sesion.AtenderAsync(token);
        }
        finally
        {
            conexion.Cerrar();
            _conexiones.TryRemove(conexion.Numero, out _);
        }
    }

    private static async Task<bool> EsperarAsync(Task tarea, string descripcion)
    {
        try
        {
            await tarea.WaitAsync(EsperaMaxima);
            return true;
        }
        catch (TimeoutException)
        {
            Console.Error.WriteLine($"El Task {descripcion} no terminó dentro del timeout");
            return false;
        }
    }

    private sealed class ConexionDeCliente
    {
        private readonly Socket _socket;

        public ConexionDeCliente(int numero, Socket socket)
        {
            Numero = numero;
            _socket = socket;
        }

        public int Numero { get; }

        // Se asigna apenas se lanza la atención del cliente.
        public Task Tarea { get; set; } = Task.CompletedTask;

        // Cerrar dos veces no es un error: el socket ya cerrado simplemente se ignora.
        public void Cerrar()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            _socket.Close();
        }
    }
}
