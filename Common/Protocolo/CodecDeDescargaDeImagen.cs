namespace Common.Protocolo;

public static class CodecDeDescargaDeImagen
{
    public static byte[] CodificarSolicitud(string nombreModelo)
        => CodecDeCampoUnico.Codificar(nombreModelo);

    public static string DecodificarSolicitud(ReadOnlySpan<byte> datos)
        => CodecDeCampoUnico.Decodificar(datos);

    public static byte[] CodificarRespuesta(string nombreSugerido)
        => CodecDeCampoUnico.Codificar(nombreSugerido);

    public static string DecodificarRespuesta(ReadOnlySpan<byte> datos)
        => CodecDeCampoUnico.Decodificar(datos);
}
