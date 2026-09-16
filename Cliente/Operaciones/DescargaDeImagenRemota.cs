using Common.Protocolo;

namespace Cliente.Operaciones;

internal sealed class DescargaDeImagenRemota
{
    private readonly ConexionConServidor _conexion;

    public DescargaDeImagenRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public string Ejecutar(string nombreModelo)
    {
        byte[] solicitud = CodecDeDescargaDeImagen.CodificarSolicitud(nombreModelo);
        byte[] cuerpo = _conexion.EnviarSolicitudYLeerCuerpo(Comandos.DescargarImagen, solicitud);
        string nombreSugerido = CodecDeDescargaDeImagen.DecodificarRespuesta(cuerpo);

        string nombreArchivo = Path.GetFileName(nombreSugerido);
        Directory.CreateDirectory(_conexion.DownloadsDirectory);
        string rutaDestino = Path.Combine(_conexion.DownloadsDirectory, nombreArchivo);

        long tamanio = _conexion.RecibirArchivoEn(rutaDestino);
        return $"Imagen guardada en '{rutaDestino}' ({tamanio} bytes)";
    }
}
