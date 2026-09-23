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
    public string ImagesDirectory { get; private init; } = string.Empty;
    public int MaxFileBytes { get; private init; }
    public IPEndPoint Endpoint => new(ListenIp, ListenPort);

    public static ConfiguracionServidor Cargar()
    {
        return new ConfiguracionServidor
        {
            ListenIp = ConfiguracionDeLaAplicacion.LeerDireccion("listenIp"),
            ListenPort = ConfiguracionDeLaAplicacion.LeerPuerto("listenPort"),
            Backlog = ConfiguracionDeLaAplicacion.LeerPositivo("backlog"),
            MaxPayloadBytes = ConfiguracionDeLaAplicacion.LeerPositivo("maxPayloadBytes"),
            JoinTimeoutMilliseconds = ConfiguracionDeLaAplicacion.LeerPositivo("joinTimeoutMilliseconds"),
            ReservationDelaySeconds = ConfiguracionDeLaAplicacion.LeerPositivo("reservationDelaySeconds"),
            ImagesDirectory = ConfiguracionDeLaAplicacion.Leer("imagesDirectory"),
            MaxFileBytes = ConfiguracionDeLaAplicacion.LeerPositivo("maxFileBytes")
        };
    }
}
