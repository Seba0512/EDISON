using System.Security.Cryptography;
using System.Text;
using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Clave de acceso de un usuario (CR1). Guarda un hash SHA-256 con salt en lugar del texto plano:
/// si el estado del servidor termina en un log o en disco, la clave original no aparece. El salt
/// hace que dos usuarios con la misma clave tengan hashes distintos. Todo con la criptografia de la
/// BCL: la letra prohibe parsers de serializacion, no <c>System.Security.Cryptography</c>.
///
/// La comparacion usa <see cref="CryptographicOperations.FixedTimeEquals"/> para no filtrar por
/// tiempo de respuesta cuantos bytes coincidieron.
/// </summary>
public class Credencial
{
    private const int LargoMinimoDeLaClave = 4;
    private const int LargoDelSalt = 16;

    private readonly byte[] salt;
    private readonly byte[] hash;

    public Credencial(string claveEnTextoPlano)
    {
        if (string.IsNullOrWhiteSpace(claveEnTextoPlano) || claveEnTextoPlano.Length < LargoMinimoDeLaClave)
        {
            throw new DominioException(
                $"La clave debe tener al menos {LargoMinimoDeLaClave} caracteres");
        }

        salt = RandomNumberGenerator.GetBytes(LargoDelSalt);
        hash = HashDe(claveEnTextoPlano, salt);
    }

    public bool Verifica(string claveIngresada)
    {
        if (string.IsNullOrEmpty(claveIngresada))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(hash, HashDe(claveIngresada, salt));
    }

    private static byte[] HashDe(string clave, byte[] salt)
    {
        byte[] bytesDeLaClave = Encoding.UTF8.GetBytes(clave);
        byte[] bytesConSalt = new byte[salt.Length + bytesDeLaClave.Length];
        salt.CopyTo(bytesConSalt, 0);
        bytesDeLaClave.CopyTo(bytesConSalt, salt.Length);
        return SHA256.HashData(bytesConSalt);
    }
}
