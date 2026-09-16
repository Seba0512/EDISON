namespace Common.Protocolo;

/// <summary>
/// Serializa el REQ y el RES OK de CMD 16 (descarga de imagen): el REQ es un unico campo de texto con
/// el nombre del modelo pedido; el RES OK es un unico campo de texto con el nombre de archivo sugerido
/// para que el cliente guarde la imagen. El archivo en si viaja fuera de frame recien despues del RES
/// (ver <see cref="RespuestaDelServidor.OkConEnvioPosterior"/>): este codec cubre unicamente los
/// campos de texto, nunca los bytes binarios.
/// </summary>
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
