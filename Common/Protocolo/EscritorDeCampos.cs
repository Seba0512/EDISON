using System.Buffers.Binary;

namespace Common.Protocolo;

/// <summary>
/// Serializa campos de texto como <c>[LARGO Int32 big-endian][UTF-8]</c>, el formato que
/// <c>protocolo-base.md</c> fija para todos los payloads con más de un campo (CR1 y CR2-CR10).
/// </summary>
public sealed class EscritorDeCampos
{
    private readonly MemoryStream _flujo = new();

    public void EscribirCampo(string valor)
    {
        byte[] texto = TextoUtf8.Codificar(valor);
        EscribirLargo(texto.Length);
        _flujo.Write(texto);
    }

    public byte[] ObtenerBytes()
        => _flujo.ToArray();

    private void EscribirLargo(int largo)
    {
        byte[] bytes = new byte[ConstantesDelProtocolo.LargoCampoLargo];
        BinaryPrimitives.WriteInt32BigEndian(bytes, largo);
        _flujo.Write(bytes);
    }
}
