using System.Net;
using Common;

namespace Servidor;

internal sealed class ConfiguracionServidor
{
    public IPAddress ListenIp { get; private init; } = IPAddress.None;
    public int ListenPort { get; private init; }
    public string ImagesDirectory { get; private init; } = string.Empty;
    public string ReceiptsDirectory { get; private init; } = string.Empty;
    public TimeSpan ReservationTimeout { get; private init; }
    public string MysqlConnectionString { get; private init; } = string.Empty;
    public IPEndPoint Endpoint => new(ListenIp, ListenPort);

    public static ConfiguracionServidor Cargar()
    {
        int plazoEnSegundos = ConfiguracionDeLaAplicacion.LeerEntero("PLAZO_RESERVA_SEGUNDOS", 30);
        if (plazoEnSegundos <= 0)
        {
            throw new ConfiguracionInvalidaException(
                "La variable 'PLAZO_RESERVA_SEGUNDOS' debe ser mayor que cero");
        }

        return new ConfiguracionServidor
        {
            ListenIp = ConfiguracionDeLaAplicacion.LeerDireccion("SERVIDOR_IP_ESCUCHA", "0.0.0.0"),
            ListenPort = ConfiguracionDeLaAplicacion.LeerPuerto("SERVIDOR_PUERTO", 8080),
            ImagesDirectory = ConfiguracionDeLaAplicacion.Leer("DIRECTORIO_IMAGENES", "imagenes"),
            ReceiptsDirectory = ConfiguracionDeLaAplicacion.Leer(
                "DIRECTORIO_COMPROBANTES", "comprobantes"),
            ReservationTimeout = TimeSpan.FromSeconds(plazoEnSegundos),
            MysqlConnectionString = ConfiguracionDeLaAplicacion.Leer(
                "MYSQL_CADENA_CONEXION",
                "Server=127.0.0.1;Port=3306;Database=edison;User=edison;Password=edison;CharSet=utf8mb4")
        };
    }
}
