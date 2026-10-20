using System.Net.Sockets;
using Common;

namespace Cliente;

internal static class Program
{
    private const string OpcionDeSalida = "S";
    private const string ConexionPerdida =
        "Se perdió la conexión con el servidor. Es posible que se haya apagado.";

    private static async Task<int> Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        try
        {
            await EjecutarAsync();
            return 0;
        }
        catch (ConfiguracionInvalidaException excepcion)
        {
            Consola.ErrorFatal($"Configuración inválida: {excepcion.Message}");
        }
        catch (ConexionCerradaException)
        {
            Consola.ErrorFatal(ConexionPerdida);
        }
        catch (ErrorInternoDelServidorException excepcion)
        {
            Consola.ErrorFatal($"Error interno del servidor: {excepcion.Message}");
        }
        catch (ProtocoloException excepcion)
        {
            Consola.ErrorFatal($"Error de protocolo: {excepcion.Message}");
        }
        // El Message de estas excepciones es el texto técnico de .NET, en inglés: no se le muestra
        // al usuario. Si el servidor se apaga, el corte llega como IOException o ConexionCerrada
        // según el momento, y en los dos casos el usuario tiene que ver el mismo aviso.
        catch (SocketException)
        {
            Consola.ErrorFatal("No fue posible conectarse al servidor.");
        }
        catch (IOException)
        {
            Consola.ErrorFatal(ConexionPerdida);
        }
        catch (UnauthorizedAccessException)
        {
            Consola.ErrorFatal("No hay permisos sobre un archivo o carpeta local.");
        }

        return 1;
    }

    private static async Task EjecutarAsync()
    {
        ConfiguracionCliente configuracion = ConfiguracionCliente.Cargar();
        bool reintentar;
        do
        {
            reintentar = await IntentarConexionYContinuarAsync(configuracion);
        } while (reintentar);
    }

    private static async Task<bool> IntentarConexionYContinuarAsync(ConfiguracionCliente configuracion)
    {
        ConexionConServidor conexion;
        try
        {
            conexion = await ConexionConServidor.ConectarAsync(configuracion);
        }
        catch (SocketException excepcion) when (
            excepcion.SocketErrorCode == SocketError.ConnectionRefused)
        {
            return ElegirSiReintentar();
        }

        await using (conexion)
        {
            Consola.Titulo("Sistema de Modelos y Reservas");
            Consola.Exito($"Conectado a {configuracion.Destino}");
            await new Menu(conexion).EjecutarAsync();
        }

        return false;
    }

    private static bool ElegirSiReintentar()
    {
        Consola.Error("No fue posible conectarse al servidor.");
        Console.WriteLine();
        Consola.Info("[Enter] Reintentar");
        Consola.Info($"[{OpcionDeSalida}] Salir");

        string? entrada = Console.ReadLine();
        while (!EsEleccionValida(entrada))
        {
            Consola.Error("Opción inválida. Presione Enter para reintentar o escriba S para salir.");
            entrada = Console.ReadLine();
        }

        return entrada is { Length: 0 };
    }

    // Fin de entrada cuenta como salir. Enter vacío reintenta.
    private static bool EsEleccionValida(string? entrada)
        => entrada is null
           || entrada.Length == 0
           || entrada.Trim().Equals(OpcionDeSalida, StringComparison.OrdinalIgnoreCase);
}
