using Common.Protocolo;

namespace Servidor.Comandos;

/// <summary>
/// Acceso a la transferencia binaria fuera de frame de la conexión actual (CMD 15/16): el mismo
/// <see cref="Stream"/> que usa el codec de tramas, más el <see cref="CanalDeArchivos"/> configurado
/// con el tamaño de bloque del servidor.
///
/// Un manejador de CMD 15 (subida) llama a <see cref="RecibirEn"/> dentro de <c>Responder</c>: el
/// codec ya consumió exactamente el REQ, así que el stream queda posicionado justo al inicio de los
/// bytes del archivo. Un manejador de CMD 16 (descarga) no puede escribir el archivo durante
/// <c>Responder</c> porque el RES todavía no salió: construye la respuesta con
/// <see cref="RespuestaDelServidor.OkConEnvioPosterior"/> y llama a <see cref="EnviarDesde"/> dentro
/// de esa acción, que <c>SesionDeCliente</c> invoca recién después de escribir el RES.
/// </summary>
internal sealed class ArchivosDeSesion
{
    private readonly Stream _stream;
    private readonly CanalDeArchivos _canal;
    private readonly long _maxFileBytes;

    public ArchivosDeSesion(Stream stream, CanalDeArchivos canal, long maxFileBytes)
    {
        _stream = stream;
        _canal = canal;
        _maxFileBytes = maxFileBytes;
    }

    public long RecibirEn(Stream destino)
        => _canal.RecibirEn(_stream, destino, _maxFileBytes);

    public void EnviarDesde(Stream origen, long tamanio)
        => _canal.Enviar(_stream, origen, tamanio);
}
