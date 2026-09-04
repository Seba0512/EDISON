using System.Security.Cryptography;
using System.Text;
using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Clave de acceso de un usuario (CR1). Guarda un resumen SHA-256 de la clave junto a un salt
/// aleatorio, nunca la clave: <see cref="Verifica"/> recalcula el resumen de lo que le pasan y lo
/// compara, asi el texto de la clave no queda en memoria ni puede salir de esta clase.
///
/// El salt es propio de cada credencial. Sin el, dos usuarios con la misma clave tendrian el mismo
/// resumen y bastaria una tabla precalculada para romper las dos de una vez.
///
/// La comparacion usa <see cref="CryptographicOperations.FixedTimeEquals"/> y no <c>SequenceEqual</c>:
/// una comparacion que corta en el primer byte distinto tarda distinto segun cuanto se acerto, y ese
/// tiempo alcanza para adivinar el resumen byte a byte.
///
/// Todo sale de <c>System.Security.Cryptography</c>, de la biblioteca estandar: no hay dependencias
/// externas, y la restriccion de la letra es sobre serializar (BinaryFormatter, parsers JSON o XML),
/// no sobre criptografia.
///
/// Separarla de <see cref="Usuario"/> deja al usuario con la identidad y a la credencial con el
/// secreto: cambiar el algoritmo (a PBKDF2, por ejemplo) se hace aca y nadie mas se entera
/// (Variaciones Protegidas).
/// </summary>
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
