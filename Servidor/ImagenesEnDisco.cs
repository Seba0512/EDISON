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
    private const string NoSePudoGuardar =
        "No se pudo guardar la imagen en el servidor. Intente nuevamente.";

    // Los FileStream se abren con useAsync: true, así ReadAsync/WriteAsync son I/O asincrónico real.
    private const int TamañoDeBuffer = 4096;

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

    // El contenido lo escribe quien llama, directo sobre el archivo: en la subida, la sesión a
    // medida que llegan las partes. Así la imagen nunca se junta entera en memoria.
    public async Task GuardarAsync(string nombreArchivo, Func<Stream, Task> escribirContenido)
    {
        string ruta = RutaDe(nombreArchivo);
        // Se escribe a un .tmp y luego se mueve, para no dejar una imagen a medias si falla la copia.
        string rutaTemporal = ruta + ".tmp";
        try
        {
            await using (FileStream destino = CrearTemporal(rutaTemporal))
            {
                await EscribirContenidoAsync(destino, escribirContenido);
            }

            Mover(rutaTemporal, ruta);
        }
        finally
        {
            IntentarEliminarTemporal(rutaTemporal);
        }
    }

    public Stream AbrirLectura(string nombreArchivo)
    {
        try
        {
            // FileShare.Delete permite borrar la imagen mientras otro cliente todavía la descarga.
            return new FileStream(
                RutaDe(nombreArchivo), FileMode.Open, FileAccess.Read,
                FileShare.Read | FileShare.Delete, TamañoDeBuffer, useAsync: true);
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            // Pasa, por ejemplo, si otro cliente reemplazó o borró la imagen justo antes.
            throw new AlmacenDeImagenesException(
                "La imagen no está disponible en este momento. Intente nuevamente.", excepcion);
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
            throw new AlmacenDeImagenesException("No se pudo eliminar la imagen del servidor.", excepcion);
        }
    }

    // Las imágenes pertenecen a modelos que viven en memoria: al apagar el servidor dejan de tener dueño.
    // Solo se borran archivos que este almacén pudo haber creado, nunca otros que estén en la carpeta.
    public void EliminarTodas()
    {
        if (!Directory.Exists(_directorio))
        {
            return;
        }

        foreach (string ruta in Directory.EnumerateFiles(_directorio))
        {
            if (!FueCreadoPorElAlmacen(ruta))
            {
                continue;
            }

            try
            {
                File.Delete(ruta);
            }
            catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
            {
                // Un archivo trabado no debe impedir que se borren los demás.
                Console.Error.WriteLine($"No se pudo eliminar la imagen '{ruta}': {excepcion.Message}");
            }
        }
    }

    private static bool FueCreadoPorElAlmacen(string ruta)
    {
        string extension = Path.GetExtension(ruta).ToLowerInvariant();
        return ExtensionesPermitidas.Contains(extension) || extension == ".tmp";
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

    private static FileStream CrearTemporal(string rutaTemporal)
    {
        try
        {
            return new FileStream(
                rutaTemporal, FileMode.Create, FileAccess.Write, FileShare.None,
                TamañoDeBuffer, useAsync: true);
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            throw new AlmacenDeImagenesException(NoSePudoGuardar, excepcion);
        }
    }

    // Solo una falla del disco se informa como error del almacén. Un corte de red a mitad de la
    // subida sigue de largo, porque esa conexión ya no puede recibir una respuesta.
    private static async Task EscribirContenidoAsync(
        Stream destino, Func<Stream, Task> escribirContenido)
    {
        try
        {
            await escribirContenido(destino);
        }
        catch (ArchivoLocalException excepcion)
        {
            throw new AlmacenDeImagenesException(NoSePudoGuardar, excepcion);
        }
    }

    private static void Mover(string rutaTemporal, string ruta)
    {
        try
        {
            File.Move(rutaTemporal, ruta, overwrite: true);
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            throw new AlmacenDeImagenesException(NoSePudoGuardar, excepcion);
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
