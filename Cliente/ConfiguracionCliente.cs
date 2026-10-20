using Common;

namespace Cliente;

internal sealed class ConfiguracionCliente
{
    // Puede ser una IP o un nombre (en Docker, el del contenedor del servidor).
    public string ServerHost { get; private init; } = string.Empty;
    public int ServerPort { get; private init; }
    public string DownloadsDirectory { get; private init; } = string.Empty;
    public string Destino => $"{ServerHost}:{ServerPort}";

    public static ConfiguracionCliente Cargar()
    {
        return new ConfiguracionCliente
        {
            ServerHost = ConfiguracionDeLaAplicacion.LeerHost("SERVIDOR_HOST", "127.0.0.1"),
            ServerPort = ConfiguracionDeLaAplicacion.LeerPuerto("SERVIDOR_PUERTO", 8080),
            DownloadsDirectory = ConfiguracionDeLaAplicacion.Leer("DIRECTORIO_DESCARGAS", "descargas")
        };
    }
}
