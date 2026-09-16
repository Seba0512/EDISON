using Common.Excepciones;

namespace Servidor.Repositorios;

/// <summary>
/// Saneamiento único de nombres de archivo de imagen (CMD 15/16). Ninguno de los dos extremos confía
/// en el nombre que recibe: el cliente propone un nombre al subir y el servidor lo valida antes de
/// tocar el disco; y aunque hoy el servidor deriva el nombre real del nombre del modelo (nunca de un
/// valor arbitrario del cliente), este saneamiento es el único punto que decide qué es un nombre de
/// archivo válido, para que no haya dos criterios distintos en el código.
/// </summary>
internal static class NombreDeArchivoSeguro
{
    private static readonly string[] ExtensionesPermitidas = { ".png", ".jpg", ".jpeg" };

    private static readonly HashSet<string> NombresReservados = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>Descarta separadores y componentes de ruta, rechaza vacíos, nombres reservados y extensiones no admitidas.</summary>
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

    /// <summary>Última verificación antes de tocar el disco: la ruta resultante debe quedar dentro de <paramref name="directorioRaiz"/>.</summary>
    public static void ExigirDentroDe(string rutaCompleta, string directorioRaiz)
    {
        string rutaCanonica = Path.GetFullPath(rutaCompleta);
        string raizCanonica = Path.GetFullPath(directorioRaiz) + Path.DirectorySeparatorChar;
        if (!rutaCanonica.StartsWith(raizCanonica, StringComparison.Ordinal))
            throw new ProtocoloException("La ruta del archivo queda fuera del directorio permitido");
    }
}
