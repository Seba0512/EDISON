namespace Common.Protocolo;

public static class CodecDeCancelacionDeReserva
{
    public static byte[] Codificar(Guid idReserva)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirGuid(idReserva);
        return escritor.ObtenerBytes();
    }

    public static Guid Decodificar(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        Guid idReserva = lector.LeerGuid();
        lector.ExigirFinDePayload();
        return idReserva;
    }
}
