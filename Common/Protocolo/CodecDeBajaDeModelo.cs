namespace Common.Protocolo;

/// <summary>
/// Serializa el pedido de CMD 12 (CR4/SR3): el REQ es un único campo de texto con el nombre del
/// modelo a eliminar. El RES es texto plano (confirmación u error), así que no hace falta codificar
/// ningún resultado: <see cref="RespuestaDelServidor"/> ya lo resuelve.
/// </summary>
public static class CodecDeBajaDeModelo
{
    public static byte[] Codificar(string nombreModelo)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCampo(nombreModelo);
        return escritor.ObtenerBytes();
    }

    public static string Decodificar(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        string nombreModelo = lector.LeerCampo();
        lector.ExigirFinDePayload();
        return nombreModelo;
    }
}
