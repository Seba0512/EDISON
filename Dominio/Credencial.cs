using System.Security.Cryptography;
using System.Text;
using Dominio.Excepciones;

namespace Dominio;

public class Credencial
{
    private const int LargoMinimoDeLaClave = 4;
    private const int LargoDelSalt = 16;

    private readonly byte[] salt;
    private readonly byte[] resumen;

    public Credencial(string clave)
    {
        ExigirClaveValida(clave);
        salt = RandomNumberGenerator.GetBytes(LargoDelSalt);
        resumen = ResumenDe(clave, salt);
    }

    public bool Verifica(string claveIngresada)
    {
        if (string.IsNullOrEmpty(claveIngresada))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(resumen, ResumenDe(claveIngresada, salt));
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
