using System.Configuration;
using System.Net.Sockets;
using Cliente.Excepciones;
using Cliente.Vistas;
using Common.Excepciones;

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
        catch (ConfigurationErrorsException exception)
        {
            Consola.ErrorFatal($"Configuración inválida: {exception.Message}");
        }
        catch (ConexionCerradaException exception)
        {
            Consola.ErrorFatal(exception.Message);
        }
        catch (ErrorInternoDelServidorException exception)
        {
            Consola.ErrorFatal($"Error interno del servidor: {exception.Message}");
        }
        catch (ProtocoloException exception)
        {
            Consola.ErrorFatal($"Error de protocolo: {exception.Message}");
        }
        catch (TransferenciaDeArchivoException exception)
        {
            Consola.ErrorFatal($"Falló la transferencia del archivo: {exception.Message}");
        }
        catch (SocketException exception)
        {
            Consola.ErrorFatal($"Error de conexión: {exception.Message}");
        }
        catch (IOException exception)
        {
            Consola.ErrorFatal($"La comunicación terminó: {exception.Message}");
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
        using (DirectorioDeDescargasDeInstancia descargas =
               new(conexion.DownloadsDirectory))
        {
            conexion.ConexionPerdida += AlPerderConexion;
            Consola.Titulo("Sistema de Modelos y Reservas");
            Consola.Exito($"Conectado a {configuracion.Endpoint}");
            AplicacionCliente.Ejecutar(conexion, descargas);
        }
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
            if (entrada is null || entrada.Trim().Equals(OpcionDeSalida, StringComparison.OrdinalIgnoreCase))
                return false;
            if (entrada.Length == 0)
                return true;

            Consola.Error("Opción inválida. Presione Enter para reintentar o escriba S para salir.");
        }
    }

    // Puede dispararse mientras el hilo principal está bloqueado esperando entrada por consola,
    // por eso avisa y termina el proceso directamente en vez de intentar volver al menú.
    private static void AlPerderConexion()
    {
        Console.Error.WriteLine();
        Consola.ErrorFatal("Se perdió la conexión con el servidor.");
        Environment.Exit(1);
    }
}
