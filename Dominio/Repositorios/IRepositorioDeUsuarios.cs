namespace Dominio.Repositorios;

/// <summary>
/// Coleccion de usuarios registrados (CR1). Se declara en el dominio y se implementa afuera (DIP):
/// el dominio dice que necesita, la infraestructura decide si eso vive en memoria o en una base.
///
/// Nunca devuelve null: <see cref="ObtenerPorNombre"/> lanza si el usuario no esta, para que ningun
/// llamador tenga que preguntar antes de usar lo que recibe.
/// La implementacion debe ser segura para Threads concurrentes y <see cref="Agregar"/> debe comprobar
/// y aplicar la unicidad del nombre dentro de una unica region critica. <see cref="Existe"/> es solo
/// una consulta; nunca debe usarse como garantia previa de unicidad.
/// </summary>
public interface IRepositorioDeUsuarios
{
    /// <exception cref="Excepciones.UsuarioDuplicadoException">Si el nombre ya esta registrado.</exception>
    void Agregar(Usuario usuario);

    /// <exception cref="Excepciones.DominioException">Si no hay un usuario con ese nombre.</exception>
    Usuario ObtenerPorNombre(string nombreUsuario);

    bool Existe(string nombreUsuario);
}
