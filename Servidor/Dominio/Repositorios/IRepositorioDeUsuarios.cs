using Servidor.Dominio.Entidades;

namespace Servidor.Dominio.Repositorios;

// Puerto de persistencia de usuarios: el dominio depende de esta interfaz y no de MySQL.
public interface IRepositorioDeUsuarios
{
    Task<Usuario?> BuscarAsync(string nombreUsuario);

    Task AgregarAsync(Usuario usuario);
}
