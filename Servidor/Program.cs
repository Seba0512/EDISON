using System.Configuration;
using System.Net.Sockets;
using Servidor.Dominio.Entidades;
using Servidor.Dominio.Fachada;

namespace Servidor;

internal static class Program
{
    private const string OrdenDeSalida = "salir";

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
        Edison edison = new(new RelojDelSistema(), configuracion.ReservationDelaySeconds);
        ImagenesEnDisco imagenes = new(configuracion.ImagesDirectory);
        EjecutorDeComandos ejecutor = new(edison, imagenes, configuracion.MaxFileBytes);
        MesaDeAprobaciones mesa = new(
            edison, TimeSpan.FromMilliseconds(configuracion.ApprovalIntervalMilliseconds));
        using ServidorTcp servidor = new(configuracion, ejecutor, mesa);
        servidor.Iniciar();
        EsperarOrdenDeSalida();
        servidor.Detener();
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
}
