using System.Configuration;
using System.Net;
using Common;

namespace Servidor;

internal sealed class ConfiguracionServidor
{
    public IPAddress ListenIp { get; private init; } = IPAddress.None;
    public int ListenPort { get; private init; }
    public int Backlog { get; private init; }
    public int MaxPayloadBytes { get; private init; }
    public int JoinTimeoutMilliseconds { get; private init; }
    public int ReservationDelaySeconds { get; private init; }
    public int ApprovalIntervalMilliseconds { get; private init; }
    public string ImagesDirectory { get; private init; } = string.Empty;
    public int MaxFileBytes { get; private init; }
    public int FileBlockBytes { get; private init; }
    public IPEndPoint Endpoint => new(ListenIp, ListenPort);

    public static ConfiguracionServidor Cargar()
    {
        return new ConfiguracionServidor
        {
            ListenIp = LeerDireccion("listenIp"),
            ListenPort = ExigirPuerto(ConfigManager.LeerEntero("listenPort")),
            Backlog = ExigirPositivo("backlog"),
            MaxPayloadBytes = ExigirPositivo("maxPayloadBytes"),
            JoinTimeoutMilliseconds = ExigirPositivo("joinTimeoutMilliseconds"),
            ReservationDelaySeconds = ExigirPositivo("reservationDelaySeconds"),
            ApprovalIntervalMilliseconds = ExigirPositivo("approvalIntervalMilliseconds"),
            ImagesDirectory = ConfigManager.Leer("imagesDirectory"),
            MaxFileBytes = ExigirPositivo("maxFileBytes"),
            FileBlockBytes = ExigirPositivo("fileBlockBytes")
        };
    }

    private static IPAddress LeerDireccion(string clave)
    {
        string valor = ConfigManager.Leer(clave);
        if (IPAddress.TryParse(valor, out IPAddress? direccion))
            return direccion;

        throw new ConfigurationErrorsException(
            $"La clave '{clave}' debe contener una dirección IP válida");
    }

    private static int ExigirPuerto(int puerto)
    {
        if (puerto is > IPEndPoint.MinPort and <= IPEndPoint.MaxPort)
            return puerto;

        throw new ConfigurationErrorsException(
            "La clave 'listenPort' debe estar entre 1 y 65535");
    }

    private static int ExigirPositivo(string clave)
    {
        int valor = ConfigManager.LeerEntero(clave);
        if (valor > 0)
            return valor;

        throw new ConfigurationErrorsException(
            $"La clave '{clave}' debe ser mayor que cero");
    }
}
