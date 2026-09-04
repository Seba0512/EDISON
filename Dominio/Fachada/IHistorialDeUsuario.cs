namespace Dominio.Fachada;

/// <summary>
/// Historial de actividades de un usuario (CR10). Va en su propia interfaz porque la usa un unico
/// manejador, el 30, y no tiene por que ver los metodos de autenticacion (ISP).
/// </summary>
public interface IHistorialDeUsuario
{
    IReadOnlyList<Actividad> HistorialDe(Usuario usuario);
}
