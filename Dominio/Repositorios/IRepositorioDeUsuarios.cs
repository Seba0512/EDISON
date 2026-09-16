namespace Dominio.Repositorios;

public interface IRepositorioDeUsuarios
{
    void Agregar(Usuario usuario);

    Usuario ObtenerPorNombre(string nombreUsuario);

    bool Existe(string nombreUsuario);
}
