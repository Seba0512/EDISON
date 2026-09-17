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
            IntentarEliminarTemporal(rutaTemporal);
            throw;
        }
    }

    public Stream AbrirLectura(string nombreArchivo)
        // FileShare.Delete permite que un borrado concurrente proceda; el archivo se libera al cerrar el último handle.
        => EnvolverErroresDeDisco(() =>
            new FileStream(RutaDe(nombreArchivo), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete));

    public bool Existe(string nombreArchivo)
        => File.Exists(RutaDe(nombreArchivo));

    public void Eliminar(string nombreArchivo)
    {
        if (nombreArchivo.Length == 0)
            return;

        string ruta = RutaDe(nombreArchivo);
        EnvolverErroresDeDisco(() => File.Delete(ruta));
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
            throw new ErrorDeAlmacenamientoDeImagenException(
                $"Error de almacenamiento accediendo a la imagen: {exception.Message}",
                exception);
        }
    }

    private static void IntentarEliminarTemporal(string rutaTemporal)
    {
        if (!File.Exists(rutaTemporal))
            return;

        try
        {
            File.Delete(rutaTemporal);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            InformarFallaDeCleanup(
                $"No se pudo limpiar el archivo temporal '{rutaTemporal}': {exception}");
        }
    }

    private static void InformarFallaDeCleanup(string mensaje)
    {
        try
        {
            Console.Error.WriteLine(mensaje);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            // El diagnóstico es best-effort: nunca debe sustituir la excepción original de Guardar.
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
