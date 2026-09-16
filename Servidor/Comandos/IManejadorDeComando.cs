using Common.Protocolo;

namespace Servidor.Comandos;

internal interface IManejadorDeComando
{
    RespuestaDelServidor Responder(SolicitudRecibida solicitud);
}
