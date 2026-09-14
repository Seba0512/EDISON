using System.Configuration;
using System.Net.Sockets;
using Common.Excepciones;

namespace Cliente;

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
        }
        catch (ConexionCerradaException exception)
        {
            Console.Error.WriteLine(exception.Message);
        }
        catch (ProtocoloException exception)
        {
            Console.Error.WriteLine($"Error de protocolo: {exception.Message}");
        }
        catch (SocketException exception)
        {
            Console.Error.WriteLine($"Error de conexión: {exception.Message}");
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine($"La comunicación terminó: {exception.Message}");
        }

        return 1;
    }

    private static void Ejecutar()
    {
        ConfiguracionCliente configuracion = ConfiguracionCliente.Cargar();
        using ConexionConServidor conexion = ConexionConServidor.Conectar(configuracion);
        Console.WriteLine($"Conectado a {configuracion.Endpoint}");
        EjecutarDiagnosticos(conexion);
    }

    private static void EjecutarDiagnosticos(ConexionConServidor conexion)
    {
        Console.WriteLine("Escriba un texto para el diagnóstico o 'salir' para cerrar.");
        string? texto;
        while ((texto = Console.ReadLine()) is not null
               && !texto.Equals("salir", StringComparison.OrdinalIgnoreCase))
        {
            string respuesta = conexion.EnviarDiagnostico(texto);
            Console.WriteLine($"Respuesta: {respuesta}");
        }
    }
}
