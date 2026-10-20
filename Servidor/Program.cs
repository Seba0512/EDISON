using System.Net.Sockets;
using System.Runtime.InteropServices;
using Common;
using Servidor.Dominio.Servicios;

namespace Servidor;

internal static class Program
{
    private const string OrdenDeSalida = "salir";

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
    }

    private static async Task EjecutarAsync()
    {
        ConfiguracionServidor configuracion = ConfiguracionServidor.Cargar();
        EstadoEnMemoria estado = new();
        ServicioDeUsuarios usuarios = new(estado);
        ServicioDeModelos modelos = new(estado);
        ServicioDeReservas reservas = new(estado, configuracion.ReservationTimeout);
        ImagenesEnDisco imagenes = new(configuracion.ImagesDirectory);
        // Si una sesión anterior terminó de golpe (cierre de consola, Ctrl+C) sus imágenes siguen en disco.
        imagenes.EliminarTodas();
        EjecutorDeComandos ejecutor = new(usuarios, modelos, reservas, imagenes);

        using CancellationTokenSource cierre = new();
        using IDisposable señales = RegistrarSeñalesDeCierre(cierre);
        await using ServidorTcp servidor = new(configuracion, ejecutor);
        try
        {
            servidor.Iniciar();
            await EsperarCierreAsync(cierre.Token);
        }
        finally
        {
            // Primero se detienen los clientes, así ningún Task está escribiendo una imagen al borrarlas.
            await servidor.DetenerAsync();
            imagenes.EliminarTodas();
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
