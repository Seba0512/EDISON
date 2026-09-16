namespace Common.Protocolo;

public static class CodecDeDescargaDeImagen
{
    public static byte[] CodificarSolicitud(string nombreModelo)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCampo(nombreModelo);
        return escritor.ObtenerBytes();
    }

    public static string DecodificarSolicitud(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        string nombreModelo = lector.LeerCampo();
        lector.ExigirFinDePayload();
        return nombreModelo;
    }

    public static byte[] CodificarRespuesta(string nombreSugerido)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCampo(nombreSugerido);
        return escritor.ObtenerBytes();
    }

    public static string DecodificarRespuesta(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        string nombreSugerido = lector.LeerCampo();
        lector.ExigirFinDePayload();
        return nombreSugerido;
    }
}
