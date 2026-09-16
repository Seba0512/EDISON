using Common.Protocolo;

namespace Servidor.Comandos;

/// <summary>
/// Espejo servidor de <c>Cliente.Menu.Opciones.OpcionNoDisponible</c>: CMD 10-30 todavía sin
/// implementación productiva. Responde PR (no ER) porque, hasta que la rama correspondiente lo
/// reemplace en <see cref="Registro.RegistroDeManejadores"/>, el comando en sí no está soportado por
/// este servidor, sea cual sea el estado de la sesión o los datos recibidos.
/// </summary>
internal sealed class ManejadorNoDisponible : IManejadorDeComando
{
    private readonly int _comando;

    public ManejadorNoDisponible(int comando)
    {
        _comando = comando;
    }

    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
        => RespuestaDelServidor.Protocolo($"CMD {_comando:D2} todavía no está disponible");
}
