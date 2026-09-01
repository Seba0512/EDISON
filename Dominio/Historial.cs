using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Actividades de un usuario en el orden en que ocurrieron (CR10). Es una Fabricacion Pura: acumular
/// el historial no es responsabilidad de <see cref="Usuario"/>, que quedaria a cargo de autenticar
/// y ademas de registrar; se extrae a una clase cohesiva que solo hace esto.
///
/// Es de solo agregado: las actividades no se modifican ni se borran, el historial es evidencia de
/// lo que paso. Solo acepta actividades de su propio usuario, asi ningun caso de uso puede mezclar
/// historiales por error.
/// </summary>
public class Historial
{
    private readonly List<Actividad> actividades = new();
    private readonly string nombreUsuario;

    public Historial(string nombreUsuario)
    {
        this.nombreUsuario = NombreDeUsuarioValidado(nombreUsuario);
    }

    public IReadOnlyList<Actividad> Actividades => actividades;

    public bool EsDe(string nombreUsuario)
    {
        return this.nombreUsuario.Equals(nombreUsuario?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public void Registrar(Actividad actividad)
    {
        if (!actividad.EsDe(nombreUsuario))
        {
            throw new DominioException(
                $"La actividad de {actividad.NombreUsuario} no pertenece al historial de {nombreUsuario}");
        }

        actividades.Add(actividad);
    }

    private static string NombreDeUsuarioValidado(string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            throw new DominioException("El historial debe pertenecer a un usuario");
        }

        return nombreUsuario.Trim();
    }
}
