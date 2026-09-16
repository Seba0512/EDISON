namespace Common.Protocolo;

public static class CodecDeCredenciales
{
    public static byte[] Codificar(Credenciales credenciales)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCampo(credenciales.NombreUsuario);
        escritor.EscribirCampo(credenciales.Clave);
        return escritor.ObtenerBytes();
    }

    public static Credenciales Decodificar(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        string nombreUsuario = lector.LeerCampo();
        string clave = lector.LeerCampo();
        lector.ExigirFinDePayload();
        return new Credenciales(nombreUsuario, clave);
    }
}
