using Dominio.Excepciones;

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
        // Se escribe a un .tmp y luego se mueve, para no dejar una imagen a medias si falla la copia.
        string rutaTemporal = ruta + ".tmp";
        try
        {
            EnvolverErroresDeDisco(() =>
            {
                using (FileStream destino = File.Create(rutaTemporal))
                    contenido.CopyTo(destino);

                File.Move(rutaTemporal, ruta, overwrite: true);
            });
        }
        catch
        {
            File.Delete(rutaTemporal);
            throw;
        }
    }

    public Stream AbrirLectura(string nombreArchivo)
        // FileShare.Delete permite que un borrado concurrente proceda; el archivo se libera al cerrar el último handle.
        => EnvolverErroresDeDisco(() =>
            new FileStream(RutaDe(nombreArchivo), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete));

    public bool Existe(string nombreArchivo)
        => File.Exists(RutaDe(nombreArchivo));

    public long TamanioDe(string nombreArchivo)
        => EnvolverErroresDeDisco(() => new FileInfo(RutaDe(nombreArchivo)).Length);

    public void Eliminar(string nombreArchivo)
    {
        if (nombreArchivo.Length == 0)
            return;

        string ruta = RutaDe(nombreArchivo);
        EnvolverErroresDeDisco(() =>
        {
            if (File.Exists(ruta))
                File.Delete(ruta);
        });
    }

    private static void EnvolverErroresDeDisco(Action accion)
    {
        EnvolverErroresDeDisco<object?>(() =>
        {
            accion();
            return null;
        });
    }

    private static T EnvolverErroresDeDisco<T>(Func<T> funcion)
    {
        try
        {
            return funcion();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new DominioException($"Error de almacenamiento accediendo a la imagen: {exception.Message}");
        }
    }

    private string RutaDe(string nombreArchivo)
    {
        string nombreSaneado = NombreDeArchivoSeguro.Validar(nombreArchivo);
        string ruta = Path.Combine(_directorio, nombreSaneado);
        NombreDeArchivoSeguro.ExigirDentroDe(ruta, _directorio);
        return ruta;
    }
}
