namespace Common.Protocolo;

/// <summary>
/// Serializa el REQ de CMD 15 (subida de imagen, CR2/CR3 con imagen): dos campos de texto, el nombre
/// del modelo destino y el nombre de archivo que el cliente propone para su imagen. El archivo en si
/// viaja fuera de frame, inmediatamente despues de este REQ y sin esperar ninguna respuesta intermedia
/// (ver <see cref="CanalDeArchivos"/> y <c>Cliente.ConexionConServidor.EnviarSolicitudConArchivo</c>):
/// este codec cubre unicamente los dos campos que van dentro del frame.
/// </summary>
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
