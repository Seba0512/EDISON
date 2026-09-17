using Cliente.Operaciones;
using Cliente.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OfertaDeSubidaDeImagen
{
    private readonly SubidaDeImagenRemota _subidaDeImagenRemota;

    public OfertaDeSubidaDeImagen(ConexionConServidor conexion)
    {
        _subidaDeImagenRemota = new SubidaDeImagenRemota(conexion);
    }

    public void Ofrecer(string nombreModelo)
    {
        string? rutaArchivo = PreguntarRuta();
        if (rutaArchivo is not null)
            Subir(nombreModelo, rutaArchivo);
    }

    // Separado de Ofrecer para poder preguntar por la imagen antes de confirmar la creación del modelo.
    public string? PreguntarRuta()
    {
        if (!LectorDeConsola.Confirmar("¿Desea subir una imagen para el modelo? (s/n):"))
            return null;

        string? rutaArchivo = LectorDeConsola.LeerTexto("Ruta local del archivo de imagen:");
        if (rutaArchivo is null)
            return null;

        rutaArchivo = SinComillas(rutaArchivo);
        if (!File.Exists(rutaArchivo))
        {
            Consola.Error($"No se encontró el archivo '{rutaArchivo}'.");
            return null;
        }

        return rutaArchivo;
    }

    // Windows "Copiar como ruta de acceso" envuelve la ruta entre comillas dobles.
    private static string SinComillas(string ruta)
        => ruta.Length >= 2 && ruta[0] == '"' && ruta[^1] == '"'
            ? ruta[1..^1]
            : ruta;

    public void Subir(string nombreModelo, string rutaArchivo)
        => Consola.Exito(_subidaDeImagenRemota.Ejecutar(nombreModelo, rutaArchivo));
}
