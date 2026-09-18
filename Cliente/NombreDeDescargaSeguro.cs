using System.Security.Cryptography;
using System.Text;
using Common.Excepciones;
using Common.Protocolo;

namespace Cliente;

internal static class NombreDeDescargaSeguro
{
    private const int LargoMaximoComponenteWindows = 255;
    private const string SufijoTemporal = ".tmp";
    private const string PrefijoNombreConHash = "modelo-";

    private static readonly HashSet<char> CaracteresInvalidos =
        new("<>:\"/\\|?*".Concat(Enumerable.Range(0, 32).Select(valor => (char)valor)));

    private static readonly HashSet<string> NombresReservados = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³"
    };

    private static readonly HashSet<string> ExtensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };

    public static string Crear(string nombreSugerido)
    {
        (string nombreLogico, string extension) = Separar(nombreSugerido);
        int largoMaximoNombre = LargoMaximoComponenteWindows - extension.Length - SufijoTemporal.Length;
        string nombreFisico = CodificarConEscape(nombreLogico);
        if (nombreFisico.Length > largoMaximoNombre)
            nombreFisico = CrearNombreConHash(nombreLogico);

        if (nombreFisico.Length > largoMaximoNombre)
            throw new ProtocoloException("El nombre de imagen anunciado es demasiado largo");

        return nombreFisico + extension;
    }

    private static (string NombreLogico, string Extension) Separar(string nombreSugerido)
    {
        int inicioExtension = nombreSugerido.LastIndexOf('.');
        if (inicioExtension <= 0)
            throw new ProtocoloException("La imagen anunciada no tiene una extensión válida");

        string nombreLogico = nombreSugerido[..inicioExtension];
        string extension = nombreSugerido[inicioExtension..];
        if (!ExtensionesPermitidas.Contains(extension))
            throw new ProtocoloException($"La extensión de imagen anunciada '{extension}' no está permitida");

        return (nombreLogico, extension);
    }

    private static string CodificarConEscape(string nombreLogico)
    {
        bool reservado = EsReservado(nombreLogico);
        StringBuilder resultado = new(nombreLogico.Length);
        for (int indice = 0; indice < nombreLogico.Length; indice++)
        {
            char caracter = nombreLogico[indice];
            bool esUltimo = indice == nombreLogico.Length - 1;
            bool debeEscaparse = caracter == '%' || CaracteresInvalidos.Contains(caracter)
                                 || esUltimo && caracter is '.' or ' '
                                 || indice == 0 && reservado;

            resultado.Append(debeEscaparse ? $"%{(int)caracter:X4}" : caracter);
        }

        return resultado.ToString();
    }

    private static string CrearNombreConHash(string nombreLogico)
    {
        byte[] hash = SHA256.HashData(TextoUtf8.Codificar(nombreLogico));
        return PrefijoNombreConHash + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool EsReservado(string nombre)
        => NombresReservados.Contains(nombre.Split('.')[0].TrimEnd(' '));
}
