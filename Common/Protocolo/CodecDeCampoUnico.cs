namespace Common.Protocolo;

// Varios comandos (baja, reserva, consulta, descarga) llevan un payload de un solo campo de texto.
// Todos codificaban y decodificaban ese campo con el mismo cuerpo repetido.
public static class CodecDeCampoUnico
{
    public static byte[] Codificar(string valor)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCampo(valor);
        return escritor.ObtenerBytes();
    }

    public static string Decodificar(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        string valor = lector.LeerCampo();
        lector.ExigirFinDePayload();
        return valor;
    }
}
