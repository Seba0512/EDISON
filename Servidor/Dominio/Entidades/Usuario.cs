using System.Security.Cryptography;
using System.Text;
using Servidor.Dominio.Excepciones;

namespace Servidor.Dominio.Entidades;

public class Usuario
{
    private const int LargoMinimoNombre = 3;
    private const int LargoMaximoNombre = 20;

    private readonly Credencial _credencial;

    public string NombreUsuario { get; }

    public Usuario(string nombreUsuario, string clave)
    {
        NombreUsuario = NombreValidado(nombreUsuario);
        _credencial = new Credencial(clave);
    }

    private Usuario(string nombreUsuario, Credencial credencial)
    {
        NombreUsuario = nombreUsuario;
        _credencial = credencial;
    }

    // Rehidrata un usuario ya registrado: lo que se guardó en la base pasó las validaciones de alta.
    internal static Usuario Restaurar(string nombreUsuario, Credencial credencial)
    {
        return new Usuario(nombreUsuario, credencial);
    }

    internal Credencial Credencial => _credencial;

    public bool SeLlama(string nombreUsuario)
    {
        return NombreUsuario.Equals(nombreUsuario?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool VerificarClave(string claveIngresada)
    {
        return _credencial.Verifica(claveIngresada);
    }

    private static string NombreValidado(string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            throw new DominioException("El nombre de usuario no puede estar vacio");
        }

        string nombreSinEspacios = nombreUsuario.Trim();
        if (nombreSinEspacios.Length is < LargoMinimoNombre or > LargoMaximoNombre)
        {
            throw new DominioException(
                $"El nombre de usuario debe tener entre {LargoMinimoNombre} y {LargoMaximoNombre} caracteres");
        }

        return nombreSinEspacios;
    }
}

public class Credencial
{
    private const int LargoMinimoDeLaClave = 4;
    private const int LargoDelSalt = 16;

    private readonly byte[] _salt;
    private readonly byte[] _resumen;

    public Credencial(string clave)
    {
        ExigirClaveValida(clave);
        // Cada credencial usa un salt distinto, así dos claves iguales no generan el mismo hash.
        _salt = RandomNumberGenerator.GetBytes(LargoDelSalt);
        _resumen = ResumenDe(clave, _salt);
    }

    private Credencial(byte[] salt, byte[] resumen)
    {
        _salt = salt;
        _resumen = resumen;
    }

    // Rehidrata la credencial con el salt y el resumen guardados: la clave original no se conoce.
    internal static Credencial Restaurar(byte[] salt, byte[] resumen)
    {
        return new Credencial(salt, resumen);
    }

    internal byte[] Salt => (byte[])_salt.Clone();

    internal byte[] Resumen => (byte[])_resumen.Clone();

    public bool Verifica(string claveIngresada)
    {
        if (string.IsNullOrEmpty(claveIngresada))
        {
            return false;
        }

        // La comparación siempre tarda lo mismo para no dar pistas de cuántos bytes coinciden.
        return CryptographicOperations.FixedTimeEquals(_resumen, ResumenDe(claveIngresada, _salt));
    }

    private static byte[] ResumenDe(string clave, byte[] saltDeLaCredencial)
    {
        byte[] claveEnBytes = Encoding.UTF8.GetBytes(clave);
        byte[] entrada = new byte[saltDeLaCredencial.Length + claveEnBytes.Length];
        saltDeLaCredencial.CopyTo(entrada, 0);
        claveEnBytes.CopyTo(entrada, saltDeLaCredencial.Length);

        return SHA256.HashData(entrada);
    }

    private static void ExigirClaveValida(string clave)
    {
        if (string.IsNullOrWhiteSpace(clave) || clave.Length < LargoMinimoDeLaClave)
        {
            throw new DominioException(
                $"La clave debe tener al menos {LargoMinimoDeLaClave} caracteres");
        }
    }
}
