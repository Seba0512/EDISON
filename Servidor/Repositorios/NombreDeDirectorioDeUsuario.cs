using System.Security.Cryptography;
using System.Text;
using Common.Excepciones;

namespace Servidor.Repositorios;

internal static class NombreDeDirectorioDeUsuario
{
    private const string NombreAlternativo = "usuario";
    private const int BytesDelSufijo = 8;
    private static readonly HashSet<char> CaracteresInvalidos =
        new(".<>:\"/\\|?*".Concat(Enumerable.Range(0, 32).Select(valor => (char)valor)));
    private static readonly HashSet<string> NombresReservados = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string Crear(string nombreUsuario)
    {
        string nombreNormalizado = (nombreUsuario ?? string.Empty).Trim().ToLowerInvariant();
        if (nombreNormalizado.Length == 0)
            throw new ProtocoloException("No se puede crear una carpeta para un usuario vacío");

        string parteVisible = Sanitizar(nombreNormalizado);
        string sufijo = CalcularSufijo(nombreNormalizado);
        return $"{parteVisible}-{sufijo}";
    }

    public static string Validar(string nombreDirectorio)
    {
        string nombre = (nombreDirectorio ?? string.Empty).Trim();
        if (nombre.Length == 0 || nombre is "." or ".." || nombre != nombreDirectorio)
            throw new ProtocoloException("La referencia interna contiene una carpeta inválida");
        if (nombre.Any(CaracteresInvalidos.Contains) || nombre.EndsWith(' '))
            throw new ProtocoloException("La referencia interna contiene una carpeta inválida");
        if (NombresReservados.Contains(Path.GetFileNameWithoutExtension(nombre)))
            throw new ProtocoloException("La referencia interna contiene una carpeta reservada");

        return nombre;
    }

    private static string Sanitizar(string nombre)
    {
        char[] caracteres = nombre
            .Select(caracter => CaracteresInvalidos.Contains(caracter) ? '_' : caracter)
            .ToArray();
        string resultado = new string(caracteres).TrimEnd(' ', '.');
        return resultado.Length == 0 ? NombreAlternativo : resultado;
    }

    private static string CalcularSufijo(string nombreNormalizado)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(nombreNormalizado));
        return Convert.ToHexString(hash, 0, BytesDelSufijo).ToLowerInvariant();
    }
}
