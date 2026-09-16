using Common.Protocolo;

namespace Cliente.Operaciones;

/// <summary>
/// CMD 16: descarga la imagen de un modelo (paso opcional dentro de "Consultar modelo", nunca una
/// opcion de menu propia). Pide el cuerpo con <see cref="ConexionConServidor.EnviarSolicitudYLeerCuerpo"/>,
/// decodifica el nombre de archivo sugerido por el servidor y lo sanea localmente con
/// <see cref="Path.GetFileName"/> antes de usarlo para armar una ruta: ningun extremo confia tal cual
/// en el nombre que le propone el otro para construir una ruta de archivo local. La ruta final queda
/// bajo <see cref="ConexionConServidor.DownloadsDirectory"/>, que se crea si todavia no existe.
/// </summary>
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
