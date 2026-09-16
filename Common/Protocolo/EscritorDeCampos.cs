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

    public void EscribirEntero(int valor)
        => EscribirCampo(FormatoDeCampos.De(valor));

    public void EscribirDecimal(decimal valor)
        => EscribirCampo(FormatoDeCampos.De(valor));

    public void EscribirFecha(DateTime valor)
        => EscribirCampo(FormatoDeCampos.DeFecha(valor));

    public void EscribirFechaHora(DateTime valor)
        => EscribirCampo(FormatoDeCampos.DeFechaHora(valor));

    public void EscribirGuid(Guid valor)
        => EscribirCampo(FormatoDeCampos.De(valor));

    /// <summary>Cantidad de elementos que abre una lista (CR7, CR9, CR10, CR13). Es un entero más con nombre propio.</summary>
    public void EscribirCantidad(int cantidad)
        => EscribirEntero(cantidad);

    public byte[] ObtenerBytes()
        => _flujo.ToArray();

    private void EscribirLargo(int largo)
    {
        byte[] bytes = new byte[ConstantesDelProtocolo.LargoCampoLargo];
        BinaryPrimitives.WriteInt32BigEndian(bytes, largo);
        _flujo.Write(bytes);
    }
}
