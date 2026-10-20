using System.Net.Sockets;
using Common;
using Servidor.Dominio.Servicios;

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

    private static void Ejecutar()
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
        using ServidorTcp servidor = new(configuracion, ejecutor);
        try
        {
            servidor.Iniciar();
            EsperarOrdenDeSalida();
        }
        finally
        {
            // Primero se detienen los clientes, así ningún Thread está escribiendo una imagen al borrarlas.
            servidor.Detener();
            imagenes.EliminarTodas();
        }
    }

    private static void EsperarOrdenDeSalida()
    {
        Console.WriteLine($"Escriba '{OrdenDeSalida}' para cerrar el servidor.");
        bool esperandoOrden = true;
        while (esperandoOrden)
        {
            string? orden = Console.ReadLine();
            // Fin de entrada (por ejemplo con la entrada redirigida) cierra igual que la orden.
            esperandoOrden = orden is not null && !EsOrdenDeSalida(orden);
            if (esperandoOrden)
                Console.WriteLine($"Orden desconocida. Escriba '{OrdenDeSalida}'.");
        }
    }

    private static bool EsOrdenDeSalida(string orden)
        => orden.Equals(OrdenDeSalida, StringComparison.OrdinalIgnoreCase);
}
