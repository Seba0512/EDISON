using System.Buffers.Binary;

namespace Common.Protocolo;

public sealed class EscritorDeCampos
{
    private readonly MemoryStream _flujo = new();

    public void EscribirCampo(string valor)
    {
        byte[] texto = TextoUtf8.Codificar(valor);
        // El LARGO se mide en bytes UTF-8, nunca con string.Length.
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
