using Common.Excepciones;

namespace Servidor.Repositorios;

// Valida el nombre para que nadie pueda salirse de la carpeta de imágenes con rutas armadas.
internal static class NombreDeArchivoSeguro
{
    private static readonly string[] ExtensionesPermitidas = { ".png", ".jpg", ".jpeg" };
    private static readonly StringComparison ComparacionDeRutas =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static string Validar(string nombrePropuesto)
    {
        string nombre = (nombrePropuesto ?? string.Empty).Trim();
        if (nombre.Length == 0)
            throw new ProtocoloException("El nombre de archivo no puede estar vacío");
        if (ContieneRuta(nombre))
            throw new ProtocoloException("Se esperaba un nombre de archivo sin directorios");

        string nombreSinExtension = Path.GetFileNameWithoutExtension(nombre);
        if (NombresReservadosDelSistema.Contiene(nombreSinExtension))
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
        string raizCanonica = ConSeparadorFinal(Path.GetFullPath(directorioRaiz));
        if (!rutaCanonica.StartsWith(raizCanonica, ComparacionDeRutas))
            throw new ProtocoloException("La ruta del archivo queda fuera del directorio permitido");
    }

    private static bool ContieneRuta(string nombre)
        => Path.IsPathRooted(nombre) || nombre.Contains('/') || nombre.Contains('\\');

    private static string ConSeparadorFinal(string ruta)
        => Path.EndsInDirectorySeparator(ruta)
            ? ruta
            : ruta + Path.DirectorySeparatorChar;
}
