using Server.Domain.Entities;

namespace Server.Domain.Repositories;

public interface IRepositorioDeUsuarios
{
    void Agregar(Usuario usuario);

    bool Existe(string nombreUsuario);

    Usuario Obtener(string nombreUsuario);
}
