namespace Common.Protocolo;

public static class CodecDeSubidaDeImagen
{
    public static byte[] CodificarSolicitud(string nombreModelo, string nombreArchivo)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCampo(nombreModelo);
        escritor.EscribirCampo(nombreArchivo);
        return escritor.ObtenerBytes();
    }

    public static (string NombreModelo, string NombreArchivo) DecodificarSolicitud(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        string nombreModelo = lector.LeerCampo();
        string nombreArchivo = lector.LeerCampo();
        lector.ExigirFinDePayload();
        return (nombreModelo, nombreArchivo);
    }
}
