using Common;

namespace Servidor;

// Un problema de disco no es un error de red ni de protocolo. Con su propia excepción el ejecutor
// lo rechaza sin cerrar la conexión y sin confundirlo con un corte de red.
internal sealed class AlmacenDeImagenesException : Exception
{
    public AlmacenDeImagenesException(string mensaje, Exception causaOriginal)
        : base(mensaje, causaOriginal)
    {
    }
}

internal sealed class ImagenesEnDisco
{
    private static readonly string[] ExtensionesPermitidas = { ".png", ".jpg", ".jpeg" };

    // Windows no permite estos nombres de archivo ni siquiera con extensión.
    private static readonly HashSet<string> NombresReservados = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private static readonly StringComparison ComparacionDeRutas =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly string _directorio;

    public ImagenesEnDisco(string directorio)
    {
        _directorio = Path.GetFullPath(directorio);
        Directory.CreateDirectory(_directorio);
    }

    public static string ValidarNombre(string nombrePropuesto)
    {
        string nombre = (nombrePropuesto ?? string.Empty).Trim();
        if (nombre.Length == 0)
        {
            throw new ProtocoloException("El nombre de archivo no puede estar vacío");
        }

        if (Path.IsPathRooted(nombre) || nombre.Contains('/') || nombre.Contains('\\'))
        {
            throw new ProtocoloException("Se esperaba un nombre de archivo sin directorios");
        }

        string nombreSinExtension = Path.GetFileNameWithoutExtension(nombre);
        if (NombresReservados.Contains(nombreSinExtension))
        {
            throw new ProtocoloException($"'{nombre}' es un nombre de archivo reservado");
        }

        string extension = Path.GetExtension(nombre).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
        {
            throw new ProtocoloException(
                $"La extensión '{extension}' no está permitida (use {string.Join(", ", ExtensionesPermitidas)})");
        }

        return nombre;
    }

    public void Guardar(string nombreArchivo, Stream contenido)
    {
        string ruta = RutaDe(nombreArchivo);
        // Se escribe a un .tmp y luego se mueve, para no dejar una imagen a medias si falla la copia.
        string rutaTemporal = ruta + ".tmp";
        try
        {
            using (FileStream destino = File.Create(rutaTemporal))
            {
                contenido.CopyTo(destino);
            }

            File.Move(rutaTemporal, ruta, overwrite: true);
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            IntentarEliminarTemporal(rutaTemporal);
            throw new AlmacenDeImagenesException(
                $"Error de almacenamiento guardando la imagen: {excepcion.Message}", excepcion);
        }
    }

    public Stream AbrirLectura(string nombreArchivo)
    {
        try
        {
            // FileShare.Delete permite borrar la imagen mientras otro cliente todavía la descarga.
            return new FileStream(
                RutaDe(nombreArchivo), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            throw new AlmacenDeImagenesException(
                $"Error de almacenamiento abriendo la imagen: {excepcion.Message}", excepcion);
        }
    }

    public void Eliminar(string nombreArchivo)
    {
        if (nombreArchivo.Length == 0)
        {
            return;
        }

        string ruta = RutaDe(nombreArchivo);
        try
        {
            File.Delete(ruta);
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            throw new AlmacenDeImagenesException(
                $"Error de almacenamiento eliminando la imagen: {excepcion.Message}", excepcion);
        }
    }

    private string RutaDe(string nombreArchivo)
    {
        string ruta = Path.GetFullPath(Path.Combine(_directorio, nombreArchivo));
        ExigirDentroDelDirectorio(ruta);
        return ruta;
    }

    // Frena rutas armadas con ".." o separadores que intenten salirse de la carpeta de imágenes.
    private void ExigirDentroDelDirectorio(string rutaCompleta)
    {
        string raizConSeparador = Path.EndsInDirectorySeparator(_directorio)
            ? _directorio
            : _directorio + Path.DirectorySeparatorChar;
        if (!rutaCompleta.StartsWith(raizConSeparador, ComparacionDeRutas))
        {
            throw new ProtocoloException("La ruta del archivo queda fuera del directorio permitido");
        }
    }

    private static void IntentarEliminarTemporal(string rutaTemporal)
    {
        try
        {
            if (File.Exists(rutaTemporal))
            {
                File.Delete(rutaTemporal);
            }
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"No se pudo limpiar el archivo temporal '{rutaTemporal}': {excepcion}");
        }
    }
}
