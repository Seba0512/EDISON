using Common.Protocolo;

namespace Servidor.Comandos;

/// <summary>CMD 00, temporal: ecoa el texto recibido (no pertenece al mapa CR1-CR10).</summary>
internal sealed class ManejadorDeDiagnostico : IManejadorDeComando
{
    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
    {
        string texto = TextoUtf8.Decodificar(solicitud.Datos.Span);
        return RespuestaDelServidor.Ok(texto);
    }
}
