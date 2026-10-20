using System.Net.Sockets;
using System.Runtime.InteropServices;
using Common;
using MySqlConnector;
using Servidor.Dominio.Repositorios;
using Servidor.Dominio.Servicios;
using Servidor.Persistencia;

namespace Servidor;

internal static class Program
{
    private const string OrdenDeSalida = "salir";
    private const int IntentosDeConexionConLaBase = 15;
    private static readonly TimeSpan DemoraEntreIntentos = TimeSpan.FromSeconds(2);

    private static async Task<int> Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        try
        {
            await EjecutarAsync();
            return 0;
        }
        catch (ConfiguracionInvalidaException exception)
        {
            Console.Error.WriteLine($"Configuración inválida: {exception.Message}");
            return 1;
        }
        catch (SocketException exception)
        {
            Console.Error.WriteLine($"No se pudo iniciar el servidor: {exception.Message}");
            return 1;
        }
        catch (MySqlException exception)
        {
            Console.Error.WriteLine($"No se pudo conectar con la base de datos: {exception.Message}");
            return 1;
        }
    }

    private static async Task EjecutarAsync()
    {
        ConfiguracionServidor configuracion = ConfiguracionServidor.Cargar();
        using CancellationTokenSource cierre = new();
        using IDisposable señales = RegistrarSeñalesDeCierre(cierre);

        FabricaDeConexionesMysql conexiones = new(configuracion.MysqlConnectionString);
        await conexiones.EsperarDisponibilidadAsync(
            IntentosDeConexionConLaBase, DemoraEntreIntentos, cierre.Token);

        // Wiring: el dominio recibe los puertos y acá se decide que los implementa MySQL.
        IRepositorioDeUsuarios repositorioDeUsuarios = new MysqlRepositorioDeUsuarios(conexiones);
        IRepositorioDeModelos repositorioDeModelos = new MysqlRepositorioDeModelos(conexiones);
        IRepositorioDeReservas repositorioDeReservas = new MysqlRepositorioDeReservas(conexiones);
        IRepositorioDeActividades repositorioDeActividades = new MysqlRepositorioDeActividades(conexiones);
        CandadoGlobal candado = new();
        ServicioDeUsuarios usuarios = new(repositorioDeUsuarios, repositorioDeActividades, candado);
        ServicioDeModelos modelos = new(repositorioDeModelos, repositorioDeReservas, candado);
        ServicioDeReservas reservas = new(
            repositorioDeReservas, repositorioDeModelos, candado, configuracion.ReservationTimeout);
        // Las imágenes pertenecen a modelos guardados en la base: no se borran al arrancar ni al cerrar.
        ImagenesEnDisco imagenes = new(configuracion.ImagesDirectory);
        EjecutorDeComandos ejecutor = new(usuarios, modelos, reservas, imagenes);

        await using ServidorTcp servidor = new(configuracion, ejecutor);
        try
        {
            servidor.Iniciar();
            await EsperarCierreAsync(cierre.Token);
        }
        finally
        {
            await servidor.DetenerAsync();
        }
    }

    // docker stop manda SIGTERM; Ctrl+C manda SIGINT. Los dos disparan el mismo cierre ordenado.
    private static IDisposable RegistrarSeñalesDeCierre(CancellationTokenSource cierre)
    {
        PosixSignalRegistration sigterm = PosixSignalRegistration.Create(
            PosixSignal.SIGTERM, contexto => PedirCierre(contexto, cierre));
        PosixSignalRegistration sigint = PosixSignalRegistration.Create(
            PosixSignal.SIGINT, contexto => PedirCierre(contexto, cierre));
        return new RegistroDeSeñales(sigterm, sigint);
    }

    private static void PedirCierre(PosixSignalContext contexto, CancellationTokenSource cierre)
    {
        // Cancel en true evita que el proceso muera de golpe: el cierre lo hace Main.
        contexto.Cancel = true;
        cierre.Cancel();
    }

    private static async Task EsperarCierreAsync(CancellationToken cierre)
    {
        try
        {
            // Sin consola interactiva (docker run sin -i) ReadLine devolvería null enseguida y el
            // servidor se cerraría al arrancar: ahí solo se espera la señal.
            if (Console.IsInputRedirected)
            {
                await Task.Delay(Timeout.Infinite, cierre);
                return;
            }

            // Console.ReadLine no tiene versión async ni cancelable, así que se lo aísla en un
            // Task.Run (única razón de ser de ese Task.Run) y se espera la orden o la señal.
            Task ordenDeSalida = Task.Run(LeerOrdenesDeConsola);
            await Task.WhenAny(ordenDeSalida, Task.Delay(Timeout.Infinite, cierre));
        }
        catch (OperationCanceledException)
        {
            // Llegó una señal de cierre. Es la salida normal desde Docker.
        }
    }

    private static void LeerOrdenesDeConsola()
    {
        Console.WriteLine($"Escriba '{OrdenDeSalida}' para cerrar el servidor.");
        bool esperandoOrden = true;
        while (esperandoOrden)
        {
            string? orden = Console.ReadLine();
            // Fin de entrada cierra igual que la orden.
            esperandoOrden = orden is not null && !EsOrdenDeSalida(orden);
            if (esperandoOrden)
                Console.WriteLine($"Orden desconocida. Escriba '{OrdenDeSalida}'.");
        }
    }

    private static bool EsOrdenDeSalida(string orden)
        => orden.Equals(OrdenDeSalida, StringComparison.OrdinalIgnoreCase);

    private sealed class RegistroDeSeñales : IDisposable
    {
        private readonly IDisposable[] _registros;

        public RegistroDeSeñales(params IDisposable[] registros)
        {
            _registros = registros;
        }

        public void Dispose()
        {
            foreach (IDisposable registro in _registros)
            {
                registro.Dispose();
            }
        }
    }
}
