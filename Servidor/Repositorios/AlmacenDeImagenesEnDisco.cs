namespace Servidor.Repositorios;

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
