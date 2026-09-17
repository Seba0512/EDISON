using System.Net;
using Common;

namespace Cliente;

internal sealed class ConfiguracionCliente
{
    public IPAddress ClientIp { get; private init; } = IPAddress.None;
    public IPAddress ServerIp { get; private init; } = IPAddress.None;
    public int ServerPort { get; private init; }
    public int MaxPayloadBytes { get; private init; }
    public string DownloadsDirectory { get; private init; } = string.Empty;
    public int MaxFileBytes { get; private init; }
    public int FileBlockBytes { get; private init; }
    public IPEndPoint LocalEndpoint => new(ClientIp, 0);
    public IPEndPoint Endpoint => new(ServerIp, ServerPort);

    public static ConfiguracionCliente Cargar()
    {
        return new ConfiguracionCliente
        {
            ClientIp = ConfiguracionDeLaAplicacion.LeerDireccion("clientIp"),
            ServerIp = ConfiguracionDeLaAplicacion.LeerDireccion("serverIp"),
            ServerPort = ConfiguracionDeLaAplicacion.LeerPuerto("serverPort"),
            MaxPayloadBytes = ConfiguracionDeLaAplicacion.LeerPositivo("maxPayloadBytes"),
            DownloadsDirectory = ConfiguracionDeLaAplicacion.Leer("downloadsDirectory"),
            MaxFileBytes = ConfiguracionDeLaAplicacion.LeerPositivo("maxFileBytes"),
            FileBlockBytes = ConfiguracionDeLaAplicacion.LeerPositivo("fileBlockBytes")
        };
    }
}
