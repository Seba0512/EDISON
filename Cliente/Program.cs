using System.Configuration;
using System.Net.Sockets;
using Cliente.Excepciones;
using Common.Excepciones;

namespace Cliente;

internal static class Program
{
    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
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
        catch (ErrorInternoDelServidorException exception)
        {
            Console.Error.WriteLine($"Error interno del servidor: {exception.Message}");
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
        AplicacionCliente.Ejecutar(conexion);
    }
}
