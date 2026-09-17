using Common.Protocolo;

namespace Cliente.Operaciones;

internal sealed class DescargaDeImagenRemota
{
    private readonly ConexionConServidor _conexion;
    private readonly DirectorioDeDescargasDeInstancia _descargas;

    public DescargaDeImagenRemota(
        ConexionConServidor conexion, DirectorioDeDescargasDeInstancia descargas)
    {
        _conexion = conexion;
        _descargas = descargas;
    }

    public string Ejecutar(string nombreModelo, string nombreUsuario)
    {
        string directorioDelUsuario = _descargas.PrepararDirectorioDe(nombreUsuario);
        byte[] solicitud = CodecDeDescargaDeImagen.CodificarSolicitud(nombreModelo);
        byte[] cuerpo = _conexion.EnviarSolicitudYLeerCuerpo(Comandos.DescargarImagen, solicitud);
        string nombreSugerido = CodecDeDescargaDeImagen.DecodificarRespuesta(cuerpo);

        string nombreArchivo = Path.GetFileName(nombreSugerido);
        string rutaDestino = Path.Combine(directorioDelUsuario, nombreArchivo);

        long tamanio = _conexion.RecibirArchivoEn(rutaDestino);
        return $"Imagen guardada en '{rutaDestino}' ({tamanio} bytes)";
    }
}
