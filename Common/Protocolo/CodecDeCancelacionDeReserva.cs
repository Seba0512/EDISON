namespace Common.Protocolo;

/// <summary>
/// Serializa el pedido de CMD 21 (CR6): el REQ es un único campo con el id de la reserva a cancelar.
/// El RES es texto plano (confirmación u error), así que no hace falta codificar ningún resultado:
/// <see cref="RespuestaDelServidor"/> ya lo resuelve.
/// </summary>
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
