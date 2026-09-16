using Common.Protocolo;

namespace Servidor.Comandos;

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
