using Common.Protocolo;

namespace Servidor.Comandos;

internal sealed class ManejadorDeDiagnostico : IManejadorDeComando
{
    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
    {
        string texto = TextoUtf8.Decodificar(solicitud.Datos.Span);
        return RespuestaDelServidor.Ok(texto);
    }
}
