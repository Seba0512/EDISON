namespace Common.Protocolo;

public static class CodecDeSolicitudDeReserva
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
