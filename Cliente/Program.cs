using System.Configuration;
using System.Net.Sockets;
using Common;

namespace Cliente;

internal static class Program
{
    private const string OpcionDeSalida = "S";

    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        try
        {
            Ejecutar();
            return 0;
        }
        catch (ConfigurationErrorsException excepcion)
        {
            Consola.ErrorFatal($"Configuración inválida: {excepcion.Message}");
        }
        catch (ConexionCerradaException excepcion)
        {
            Consola.ErrorFatal(excepcion.Message);
        }
        catch (ErrorInternoDelServidorException excepcion)
        {
            Consola.ErrorFatal($"Error interno del servidor: {excepcion.Message}");
        }
        catch (ProtocoloException excepcion)
        {
            Consola.ErrorFatal($"Error de protocolo: {excepcion.Message}");
        }
        catch (SocketException excepcion)
        {
            Consola.ErrorFatal($"Error de conexión: {excepcion.Message}");
        }
        catch (IOException excepcion)
        {
            Consola.ErrorFatal($"La comunicación terminó: {excepcion.Message}");
        }
        catch (UnauthorizedAccessException excepcion)
        {
            Consola.ErrorFatal($"Sin permisos sobre un archivo o carpeta local: {excepcion.Message}");
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
        catch (SocketException excepcion) when (
            excepcion.SocketErrorCode == SocketError.ConnectionRefused)
        {
            return ElegirSiReintentar();
        }

        using (conexion)
        {
            Consola.Titulo("Sistema de Modelos y Reservas");
            Consola.Exito($"Conectado a {configuracion.Endpoint}");
            new Menu(conexion).Ejecutar();
        }

        return false;
    }

    private static bool ElegirSiReintentar()
    {
        Consola.Error("No fue posible conectarse al servidor.");
        Console.WriteLine();
        Consola.Info("[Enter] Reintentar");
        Consola.Info($"[{OpcionDeSalida}] Salir");

        while (true)
        {
            string? entrada = Console.ReadLine();
            if (entrada is null
                || entrada.Trim().Equals(OpcionDeSalida, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (entrada.Length == 0)
                return true;

            Consola.Error("Opción inválida. Presione Enter para reintentar o escriba S para salir.");
        }
    }
}
