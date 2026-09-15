using System.Net.Sockets;
using Common.Protocolo;
using Dominio;
using Dominio.Fachada;
using Dominio.Tiempo;
using Servidor.Comandos;
using Servidor.Red;
using Servidor.Repositorios;

namespace Servidor;

internal sealed class ServidorTcp : IDisposable
{
    private readonly ConfiguracionServidor _configuracion;
    private readonly RegistroDeConexiones _registro = new();
    private readonly CodecDeTramas _codec;
    private readonly DespachadorDeComandos _despachador;
    private Socket? _listener;
    private Thread? _threadAceptador;
    private int _ejecutando;
    private int _siguienteCliente;

    public ServidorTcp(ConfiguracionServidor configuracion)
    {
        _configuracion = configuracion;
        _codec = new CodecDeTramas(configuracion.MaxPayloadBytes);
        _despachador = CrearDespachador();
    }

    public void Iniciar()
    {
        if (Interlocked.CompareExchange(ref _ejecutando, 1, 0) != 0)
            throw new InvalidOperationException("El servidor ya fue iniciado");

        try
        {
            _listener = CrearListener();
            _threadAceptador = new Thread(AceptarConexiones)
            {
                Name = "Aceptador TCP"
            };
            _threadAceptador.Start();
            Console.WriteLine($"Servidor escuchando en {_configuracion.Endpoint}");
        }
        catch
        {
            Interlocked.Exchange(ref _ejecutando, 0);
            CerrarListener();
            throw;
        }
    }

    public void Detener()
    {
        if (Interlocked.Exchange(ref _ejecutando, 0) == 0)
            return;

        Console.WriteLine("Cerrando servidor...");
        CerrarListener();
        UnirAceptador();
        ConexionActiva[] conexiones = _registro.TomarSnapshot();
        foreach (ConexionActiva conexion in conexiones)
            conexion.Cerrar();
        foreach (ConexionActiva conexion in conexiones)
            UnirCliente(conexion);
        Console.WriteLine("Servidor cerrado ordenadamente.");
    }

    public void Dispose()
        => Detener();

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
        while (Volatile.Read(ref _ejecutando) == 1)
        {
            try
            {
                Socket socket = ObtenerListener().Accept();
                IncorporarCliente(socket);
            }
            catch (ObjectDisposedException) when (Volatile.Read(ref _ejecutando) == 0)
            {
                return;
            }
            catch (SocketException) when (Volatile.Read(ref _ejecutando) == 0)
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
        if (Volatile.Read(ref _ejecutando) == 0)
        {
            socket.Dispose();
            return;
        }

        int numero = Interlocked.Increment(ref _siguienteCliente);
        ConexionActiva conexion = new(numero, socket);
        Thread thread = new(() => Atender(conexion))
        {
            Name = $"Cliente {numero}"
        };
        conexion.AsignarThread(thread);
        _registro.Agregar(conexion);
        IniciarCliente(conexion);
    }

    private void IniciarCliente(ConexionActiva conexion)
    {
        try
        {
            conexion.Iniciar();
        }
        catch
        {
            _registro.Quitar(conexion);
            conexion.Cerrar();
            throw;
        }
    }

    private void Atender(ConexionActiva conexion)
    {
        try
        {
            new SesionDeCliente(conexion, _codec, _despachador, new EstadoDeSesion()).Atender();
        }
        finally
        {
            _registro.Quitar(conexion);
        }
    }

    private void CerrarListener()
        => Interlocked.Exchange(ref _listener, null)?.Dispose();

    private Socket ObtenerListener()
        => _listener ?? throw new ObjectDisposedException(nameof(ServidorTcp));

    private void UnirAceptador()
    {
        Thread? thread = _threadAceptador;
        if (thread is not null && !thread.Join(_configuracion.JoinTimeoutMilliseconds))
            Console.Error.WriteLine("El Thread aceptador no terminó dentro del timeout");
    }

    private void UnirCliente(ConexionActiva conexion)
    {
        TimeSpan timeout = TimeSpan.FromMilliseconds(
            _configuracion.JoinTimeoutMilliseconds);
        if (!conexion.Unir(timeout))
            Console.Error.WriteLine(
                $"El Thread del cliente {conexion.Numero} no terminó dentro del timeout");
    }

    private static DespachadorDeComandos CrearDespachador()
    {
        RegistroDeUsuarios registroDeUsuarios = new(
            new RepositorioDeUsuariosEnMemoria(), new RelojDelSistema());
        return new DespachadorDeComandos(CrearManejadores(registroDeUsuarios));
    }

    private static IReadOnlyDictionary<int, IManejadorDeComando> CrearManejadores(
        IAutenticacion autenticacion)
        => new Dictionary<int, IManejadorDeComando>
        {
            [Common.Protocolo.Comandos.Diagnostico] = new ManejadorDeDiagnostico(),
            [Common.Protocolo.Comandos.Registrar] = new ManejadorDeRegistro(autenticacion),
            [Common.Protocolo.Comandos.IniciarSesion] = new ManejadorDeInicioDeSesion(autenticacion),
            [Common.Protocolo.Comandos.CerrarSesion] = new ManejadorDeCierreDeSesion(autenticacion)
        };
}
