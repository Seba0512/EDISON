namespace Servidor.Repositorios;

/// <summary>
/// Implementación en disco de <see cref="IRepositorioDeImagenes"/> y de
/// <see cref="Dominio.IAlmacenDeImagenes"/> (SR3): un único almacén sirve tanto para guardar y leer
/// imágenes por red (CMD 15/16) como para que el catálogo pida borrarlas al eliminar un modelo, sin
/// que el dominio conozca el sistema de archivos.
/// </summary>
internal sealed class AlmacenDeImagenesEnDisco : IRepositorioDeImagenes, Dominio.IAlmacenDeImagenes
{
    private readonly string _directorio;

    public AlmacenDeImagenesEnDisco(string directorio)
    {
        _directorio = Path.GetFullPath(directorio);
        Directory.CreateDirectory(_directorio);
    }

    public void Guardar(string nombreArchivo, Stream contenido)
    {
        string ruta = RutaDe(nombreArchivo);
        string rutaTemporal = ruta + ".tmp";
        try
        {
            using (FileStream destino = File.Create(rutaTemporal))
                contenido.CopyTo(destino);

            File.Move(rutaTemporal, ruta, overwrite: true);
        }
        catch
        {
            File.Delete(rutaTemporal);
            throw;
        }
    }

    public Stream AbrirLectura(string nombreArchivo)
        => File.OpenRead(RutaDe(nombreArchivo));

    public bool Existe(string nombreArchivo)
        => File.Exists(RutaDe(nombreArchivo));

    public long TamanioDe(string nombreArchivo)
        => new FileInfo(RutaDe(nombreArchivo)).Length;

    /// <summary>No falla si el archivo no está: el objetivo de <see cref="Dominio.IAlmacenDeImagenes"/> es que después no exista.</summary>
    public void Eliminar(string nombreArchivo)
    {
        if (nombreArchivo.Length == 0)
            return;

        string ruta = RutaDe(nombreArchivo);
        if (File.Exists(ruta))
            File.Delete(ruta);
    }

    private string RutaDe(string nombreArchivo)
    {
        string nombreSaneado = NombreDeArchivoSeguro.Validar(nombreArchivo);
        string ruta = Path.Combine(_directorio, nombreSaneado);
        NombreDeArchivoSeguro.ExigirDentroDe(ruta, _directorio);
        return ruta;
    }
}
