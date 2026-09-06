namespace Dominio.Fachada;

/// <summary>
/// Historial de actividades de un usuario (CR10). Va en su propia interfaz para que quien solo
/// consulta actividades no dependa de los metodos de autenticacion (ISP).
/// </summary>
public interface IHistorialDeUsuario
{
    IReadOnlyList<Actividad> HistorialDe(Usuario usuario);
}
