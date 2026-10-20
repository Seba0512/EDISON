using System.Net;
using Common;
using MySqlConnector;

namespace Servidor;

internal sealed class ConfiguracionServidor
{
    // Un día alcanza de sobra para cualquier demo y queda lejos del máximo que admite Task.Delay
    // (unos 49 días): con más, las esperas fallarían y las reservas quedarían pendientes para siempre.
    private const int PlazoMaximoEnSegundos = 24 * 60 * 60;
    private const string CadenaDeConexionPorDefecto =
        "Server=127.0.0.1;Port=3306;Database=edison;User=edison;Password=edison;CharSet=utf8mb4";

    public IPAddress ListenIp { get; private init; } = IPAddress.None;
    public int ListenPort { get; private init; }
    public string ImagesDirectory { get; private init; } = string.Empty;
    public string ReceiptsDirectory { get; private init; } = string.Empty;
    public TimeSpan ReservationTimeout { get; private init; }
    public string MysqlConnectionString { get; private init; } = string.Empty;
    public IPEndPoint Endpoint => new(ListenIp, ListenPort);

    public static ConfiguracionServidor Cargar()
    {
        return new ConfiguracionServidor
        {
            ListenIp = ConfiguracionDeLaAplicacion.LeerDireccion("SERVIDOR_IP_ESCUCHA", "0.0.0.0"),
            ListenPort = ConfiguracionDeLaAplicacion.LeerPuerto("SERVIDOR_PUERTO", 8080),
            ImagesDirectory = ConfiguracionDeLaAplicacion.Leer("DIRECTORIO_IMAGENES", "imagenes"),
            ReceiptsDirectory = ConfiguracionDeLaAplicacion.Leer(
                "DIRECTORIO_COMPROBANTES", "comprobantes"),
            ReservationTimeout = LeerPlazoDeResolucion(),
            MysqlConnectionString = LeerCadenaDeConexion()
        };
    }

    private static TimeSpan LeerPlazoDeResolucion()
    {
        int plazoEnSegundos = ConfiguracionDeLaAplicacion.LeerEntero("PLAZO_RESERVA_SEGUNDOS", 30);
        if (plazoEnSegundos is <= 0 or > PlazoMaximoEnSegundos)
        {
            throw new ConfiguracionInvalidaException(
                $"La variable 'PLAZO_RESERVA_SEGUNDOS' debe estar entre 1 y {PlazoMaximoEnSegundos}");
        }

        return TimeSpan.FromSeconds(plazoEnSegundos);
    }

    // Se valida al arrancar: una cadena mal formada se informa como configuración inválida, y no como
    // una excepción de .NET la primera vez que se abre una conexión.
    private static string LeerCadenaDeConexion()
    {
        string cadena = ConfiguracionDeLaAplicacion.Leer("MYSQL_CADENA_CONEXION", CadenaDeConexionPorDefecto);
        try
        {
            _ = new MySqlConnectionStringBuilder(cadena);
            return cadena;
        }
        catch (ArgumentException excepcion)
        {
            throw new ConfiguracionInvalidaException(
                $"La variable 'MYSQL_CADENA_CONEXION' no es una cadena de conexión válida: {excepcion.Message}");
        }
    }
}
