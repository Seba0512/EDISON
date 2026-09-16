using Common.Protocolo;

namespace Cliente.Operaciones;

internal sealed class SubidaDeImagenRemota
{
    private readonly ConexionConServidor _conexion;

    public SubidaDeImagenRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public string Ejecutar(string nombreModelo, string rutaArchivoLocal)
    {
        string nombreArchivo = Path.GetFileName(rutaArchivoLocal);
        byte[] solicitud = CodecDeSubidaDeImagen.CodificarSolicitud(nombreModelo, nombreArchivo);
        return _conexion.EnviarSolicitudConArchivo(Comandos.SubirImagen, solicitud, rutaArchivoLocal);
    }
}
