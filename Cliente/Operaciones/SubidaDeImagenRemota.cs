using Common.Protocolo;

namespace Cliente.Operaciones;

/// <summary>
/// CMD 15: sube la imagen de un modelo (paso opcional dentro de "Alta de modelo" y "Modificar
/// modelo", nunca una opcion de menu propia). No conoce bytes ni sockets: delega en
/// <see cref="ConexionConServidor.EnviarSolicitudConArchivo"/>, que manda el REQ y, a continuacion y
/// fuera de frame, el archivo completo, sin esperar ninguna respuesta intermedia.
/// </summary>
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
