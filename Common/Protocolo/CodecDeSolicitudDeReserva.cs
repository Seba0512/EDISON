namespace Common.Protocolo;

/// <summary>
/// Serializa el pedido de CMD 20 (CR5): el REQ es un único campo de texto con el nombre del modelo a
/// reservar. La respuesta viaja como texto plano (el Guid de la reserva dentro del mensaje de OK), así
/// que no hace falta un codec de resultado: no hay campos binarios que decodificar del lado del
/// cliente.
/// </summary>
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
