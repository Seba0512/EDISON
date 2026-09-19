using Common;

namespace Server.Commands;

internal interface IManejadorDeComando
{
    int Comando { get; }

    RespuestaDelServidor Ejecutar(Trama solicitud, ContextoDeConexion conexion);
}
