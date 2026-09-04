using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Clave de acceso de un usuario (CR1). Valida el largo minimo al crearse y es la unica que puede
/// compararla: el texto de la clave no sale de aca. Separarla de <see cref="Usuario"/> deja al
/// usuario con la identidad y a la credencial con el secreto; si maniana se decide hashear, cambia
/// esta clase y nadie mas se entera (Variaciones Protegidas).
/// </summary>
public class Credencial
{
    private const int LargoMinimoDeLaClave = 4;

    private readonly string clave;

    public Credencial(string clave)
    {
        if (string.IsNullOrWhiteSpace(clave) || clave.Length < LargoMinimoDeLaClave)
        {
            throw new DominioException(
                $"La clave debe tener al menos {LargoMinimoDeLaClave} caracteres");
        }

        this.clave = clave;
    }

    public bool Verifica(string claveIngresada)
    {
        return clave.Equals(claveIngresada, StringComparison.Ordinal);
    }
}
