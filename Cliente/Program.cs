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
        catch (TransferenciaDeArchivoException exception)
        {
            Console.Error.WriteLine($"Falló la transferencia del archivo: {exception.Message}");
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
        bool reintentar;
        do
        {
            reintentar = IntentarConexionYContinuar(configuracion);
        } while (reintentar);
    }

    private static bool IntentarConexionYContinuar(ConfiguracionCliente configuracion)
    {
        ConexionConServidor conexion;
        try
        {
            conexion = ConexionConServidor.Conectar(configuracion);
        }
        catch (SocketException exception) when (
            exception.SocketErrorCode == SocketError.ConnectionRefused)
        {
            return ElegirSiReintentar();
        }

        ContinuarCon(conexion, configuracion);
        return false;
    }

    private static void ContinuarCon(
        ConexionConServidor conexion, ConfiguracionCliente configuracion)
    {
        using (conexion)
        {
            conexion.ConexionPerdida += AlPerderConexion;
            Console.WriteLine($"Conectado a {configuracion.Endpoint}");
            AplicacionCliente.Ejecutar(conexion);
        }
    }

    private static bool ElegirSiReintentar()
    {
        Console.WriteLine("No fue posible conectarse al servidor.");
        Console.WriteLine();
        Console.WriteLine("[Enter] Reintentar");
        Console.WriteLine("[S] Salir");

        while (true)
        {
            string? entrada = Console.ReadLine();
            if (entrada is null || entrada.Trim().Equals("S", StringComparison.OrdinalIgnoreCase))
                return false;
            if (entrada.Length == 0)
                return true;

            Console.WriteLine("Opción inválida. Presione Enter para reintentar o escriba S para salir.");
        }
    }

    // Puede dispararse mientras el hilo principal está bloqueado esperando entrada por consola,
    // por eso avisa y termina el proceso directamente en vez de intentar volver al menú.
    private static void AlPerderConexion()
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Se perdió la conexión con el servidor.");
        Environment.Exit(1);
    }
}
