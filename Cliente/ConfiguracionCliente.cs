using System.Configuration;
using System.Net;
using Common;

namespace Cliente;

internal sealed class ConfiguracionCliente
{
    public IPAddress ServerIp { get; private init; } = IPAddress.None;
    public int ServerPort { get; private init; }
    public int MaxPayloadBytes { get; private init; }
    public IPEndPoint Endpoint => new(ServerIp, ServerPort);

    public static ConfiguracionCliente Cargar()
    {
        int maxPayload = ConfigManager.LeerEntero("maxPayloadBytes");
        if (maxPayload <= 0)
            throw new ConfigurationErrorsException(
                "La clave 'maxPayloadBytes' debe ser mayor que cero");

        return new ConfiguracionCliente
        {
            ServerIp = LeerDireccion("serverIp"),
            ServerPort = ExigirPuerto(ConfigManager.LeerEntero("serverPort")),
            MaxPayloadBytes = maxPayload
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
            "La clave 'serverPort' debe estar entre 1 y 65535");
    }
}
