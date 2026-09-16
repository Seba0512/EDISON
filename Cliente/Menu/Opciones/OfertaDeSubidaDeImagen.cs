using Cliente.Operaciones;

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
        if (!LectorDeConsola.Confirmar("¿Desea subir una imagen para el modelo? (s/n):"))
            return;

        string? rutaArchivo = LectorDeConsola.LeerTexto("Ruta local del archivo de imagen:");
        if (rutaArchivo is null)
            return;

        if (!File.Exists(rutaArchivo))
        {
            Console.WriteLine($"No se encontró el archivo '{rutaArchivo}'.");
            return;
        }

        Console.WriteLine(_subidaDeImagenRemota.Ejecutar(nombreModelo, rutaArchivo));
    }
}
