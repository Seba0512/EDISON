using Common.Excepciones;
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

    public string Guardar(string nombreUsuarioPropietario, string nombreArchivo, Stream contenido)
    {
        string referencia = CrearReferencia(nombreUsuarioPropietario, nombreArchivo);
        string ruta = RutaDe(referencia);
        // Se escribe a un .tmp y luego se mueve, para no dejar una imagen a medias si falla la copia.
        string rutaTemporal = ruta + ".tmp";
        try
        {
            EnvolverErroresDeDisco(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
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

        return referencia;
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
            DiagnosticoDeLimpieza.Informar(
                $"No se pudo limpiar el archivo temporal '{rutaTemporal}': {exception}");
        }
    }

    private static string CrearReferencia(string nombreUsuarioPropietario, string nombreArchivo)
    {
        string directorio = NombreDeDirectorioDeUsuario.Crear(nombreUsuarioPropietario);
        string nombreSaneado = NombreDeArchivoSeguro.Validar(nombreArchivo);
        return $"{directorio}/{nombreSaneado}";
    }

    private string RutaDe(string referencia)
    {
        (string directorio, string nombreArchivo) = SepararReferencia(referencia);
        string ruta = Path.Combine(_directorio, directorio, nombreArchivo);
        NombreDeArchivoSeguro.ExigirDentroDe(ruta, _directorio);
        return ruta;
    }

    private static (string Directorio, string NombreArchivo) SepararReferencia(string referencia)
    {
        string referenciaRecibida = referencia ?? string.Empty;
        string[] partes = referenciaRecibida.Split('/');
        if (partes.Length != 2 || referenciaRecibida.Contains('\\'))
            throw new ProtocoloException("La referencia interna de imagen es inválida");

        string directorio = NombreDeDirectorioDeUsuario.Validar(partes[0]);
        string nombreArchivo = NombreDeArchivoSeguro.Validar(partes[1]);
        return (directorio, nombreArchivo);
    }
}
