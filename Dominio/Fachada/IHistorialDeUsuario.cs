namespace Dominio.Fachada;

public interface IHistorialDeUsuario
{
    IReadOnlyList<Actividad> HistorialDe(Usuario usuario);
}
