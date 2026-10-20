using Servidor.Dominio.Entidades;

namespace Servidor.Dominio.Repositorios;

public interface IRepositorioDeActividades
{
    Task RegistrarAsync(string nombreUsuario, Actividad actividad);

    // En orden cronológico.
    Task<IReadOnlyList<Actividad>> ListarDeAsync(string nombreUsuario);
}
