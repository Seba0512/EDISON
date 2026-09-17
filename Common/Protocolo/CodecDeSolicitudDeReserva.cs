namespace Common.Protocolo;

public static class CodecDeSolicitudDeReserva
{
    public static byte[] Codificar(string nombreModelo)
        => CodecDeCampoUnico.Codificar(nombreModelo);

    public static string Decodificar(ReadOnlySpan<byte> datos)
        => CodecDeCampoUnico.Decodificar(datos);
}
