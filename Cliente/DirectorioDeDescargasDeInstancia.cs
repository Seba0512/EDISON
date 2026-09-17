using System.Security.Cryptography;
using System.Text;

namespace Cliente;

internal sealed class DirectorioDeDescargasDeInstancia : IDisposable
{
    private const string NombreAlternativo = "usuario";
    private readonly string _rutaDeInstancia;
    private int _limpiado;

    public DirectorioDeDescargasDeInstancia(string downloadsDirectory)
    {
        string raiz = Path.GetFullPath(downloadsDirectory);
        _rutaDeInstancia = Path.Combine(raiz, $"sesion-{Guid.NewGuid():N}");
        AppDomain.CurrentDomain.ProcessExit += AlCerrarProceso;
    }

    public string PrepararDirectorioDe(string nombreUsuario)
    {
        string nombreSeguro = CrearNombreSeguro(nombreUsuario);
        string ruta = Path.Combine(_rutaDeInstancia, nombreSeguro);
        Directory.CreateDirectory(ruta);
        return ruta;
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.ProcessExit -= AlCerrarProceso;
        IntentarLimpiar();
    }

    private void AlCerrarProceso(object? sender, EventArgs args)
        => IntentarLimpiar();

    private void IntentarLimpiar()
    {
        if (Interlocked.Exchange(ref _limpiado, 1) != 0 || !Directory.Exists(_rutaDeInstancia))
            return;

        try
        {
            Directory.Delete(_rutaDeInstancia, recursive: true);
        }
        catch (IOException exception)
        {
            InformarFallaDeLimpieza(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            InformarFallaDeLimpieza(exception.Message);
        }
    }

    private static string CrearNombreSeguro(string nombreUsuario)
    {
        string nombreNormalizado = nombreUsuario.Trim().ToLowerInvariant();
        char[] caracteres = nombreNormalizado
            .Select(caracter => EsInvalido(caracter) ? '_' : caracter)
            .ToArray();
        string nombreLegible = new string(caracteres).TrimEnd('.', ' ');
        if (nombreLegible.Length == 0)
            nombreLegible = NombreAlternativo;

        return $"{nombreLegible}-{CrearIdentificador(nombreNormalizado)}";
    }

    private static bool EsInvalido(char caracter)
        => caracter < ' '
           || Path.GetInvalidFileNameChars().Contains(caracter)
           || "<>:\"/\\|?*".Contains(caracter);

    private static string CrearIdentificador(string nombreUsuario)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(nombreUsuario));
        return Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
    }

    private static void InformarFallaDeLimpieza(string detalle)
        => Console.Error.WriteLine($"No se pudo limpiar la carpeta de descargas de esta instancia: {detalle}");
}
