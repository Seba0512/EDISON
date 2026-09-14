using System.Configuration;
using System.Net.Sockets;

namespace Servidor;

internal static class Program
{
    private static int Main()
    {
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
        using ServidorTcp servidor = new(configuracion);
        servidor.Iniciar();
        EsperarOrdenDeSalida();
        servidor.Detener();
    }

    private static void EsperarOrdenDeSalida()
    {
        Console.WriteLine("Escriba 'salir' para cerrar el servidor.");
        string? orden;
        do
        {
            orden = Console.ReadLine();
            if (orden is not null && !orden.Equals("salir", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine("Orden desconocida. Escriba 'salir'.");
        } while (orden is not null && !orden.Equals("salir", StringComparison.OrdinalIgnoreCase));
    }
}
