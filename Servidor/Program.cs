using System.Configuration;
using System.Net.Sockets;
using Dominio;
using Dominio.Fachada;
using Dominio.Tiempo;
using Servidor.Comandos;
using Servidor.Comandos.Manejadores;
using Servidor.Repositorios;

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
        IAutenticacion autenticacion = new RegistroDeUsuarios(
            new RepositorioDeUsuariosEnMemoria(), new RelojDelSistema());
        DespachadorDeComandos despachador = CrearDespachador(autenticacion);
        using ServidorTcp servidor = new ServidorTcp(configuracion, despachador);
        servidor.Iniciar();
        EsperarOrdenDeSalida();
        servidor.Detener();
    }

    private static DespachadorDeComandos CrearDespachador(IAutenticacion autenticacion)
        => new DespachadorDeComandos(
            new IManejadorDeComando[]
            {
                new ManejadorDeDiagnostico(),
                new ManejadorDeRegistro(autenticacion),
                new ManejadorDeInicioDeSesion(autenticacion),
                new ManejadorDeCierreDeSesion(autenticacion)
            });

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
