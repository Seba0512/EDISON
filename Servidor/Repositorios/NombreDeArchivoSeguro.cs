using Common.Excepciones;

namespace Servidor.Repositorios;

// Valida el nombre para que nadie pueda salirse de la carpeta de imágenes con rutas armadas.
internal static class NombreDeArchivoSeguro
{
    private static readonly string[] ExtensionesPermitidas = { ".png", ".jpg", ".jpeg" };

    private static readonly HashSet<string> NombresReservados = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string Validar(string nombrePropuesto)
    {
        string nombre = Path.GetFileName((nombrePropuesto ?? string.Empty).Trim());
        if (nombre.Length == 0)
            throw new ProtocoloException("El nombre de archivo no puede estar vacío");

        string nombreSinExtension = Path.GetFileNameWithoutExtension(nombre);
        if (NombresReservados.Contains(nombreSinExtension))
            throw new ProtocoloException($"'{nombre}' es un nombre de archivo reservado");

        string extension = Path.GetExtension(nombre).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
            throw new ProtocoloException(
                $"La extensión '{extension}' no está permitida (use {string.Join(", ", ExtensionesPermitidas)})");

        return nombre;
    }

    public static void ExigirDentroDe(string rutaCompleta, string directorioRaiz)
    {
        string rutaCanonica = Path.GetFullPath(rutaCompleta);
        string raizCanonica = Path.GetFullPath(directorioRaiz) + Path.DirectorySeparatorChar;
        if (!rutaCanonica.StartsWith(raizCanonica, StringComparison.Ordinal))
            throw new ProtocoloException("La ruta del archivo queda fuera del directorio permitido");
    }
}
