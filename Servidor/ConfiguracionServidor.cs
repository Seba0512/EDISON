using System.Net;
using Common;

namespace Servidor;

internal sealed class ConfiguracionServidor
{
    public IPAddress ListenIp { get; private init; } = IPAddress.None;
    public int ListenPort { get; private init; }
    public string ImagesDirectory { get; private init; } = string.Empty;
    public IPEndPoint Endpoint => new(ListenIp, ListenPort);

    public static ConfiguracionServidor Cargar()
    {
        return new ConfiguracionServidor
        {
            ListenIp = ConfiguracionDeLaAplicacion.LeerDireccion("listenIp"),
            ListenPort = ConfiguracionDeLaAplicacion.LeerPuerto("listenPort"),
            ImagesDirectory = ConfiguracionDeLaAplicacion.Leer("imagesDirectory")
        };
    }
}
