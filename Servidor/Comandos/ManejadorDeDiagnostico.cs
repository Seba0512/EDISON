using Common.Protocolo;
using Servidor.Red;

namespace Servidor.Comandos;

/// <summary>CMD 00, temporal: ecoa el texto recibido (no pertenece al mapa CR1-CR10).</summary>
internal sealed class ManejadorDeDiagnostico : IManejadorDeComando
{
    public RespuestaDelServidor Responder(ReadOnlyMemory<byte> datos, EstadoDeSesion sesion)
    {
        string texto = TextoUtf8.Decodificar(datos.Span);
        return RespuestaDelServidor.Ok(texto);
    }
}
