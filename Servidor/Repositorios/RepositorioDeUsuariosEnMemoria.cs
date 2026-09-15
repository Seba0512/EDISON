using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Servidor.Repositorios;

/// <summary>
/// Implementación productiva de <see cref="IRepositorioDeUsuarios"/> (CR1): usuarios en memoria,
/// vivos mientras el proceso del servidor esté en pie.
/// </summary>
internal sealed class RepositorioDeUsuariosEnMemoria : IRepositorioDeUsuarios
{
    private readonly object _candado = new();
    private readonly Dictionary<string, Usuario> _usuarios = new(StringComparer.OrdinalIgnoreCase);

    public void Agregar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        string nombre = usuario.NombreUsuario;
        lock (_candado)
        {
            if (_usuarios.ContainsKey(nombre))
                throw new UsuarioDuplicadoException(nombre);

            _usuarios.Add(nombre, usuario);
        }
    }

    public Usuario ObtenerPorNombre(string nombreUsuario)
    {
        lock (_candado)
        {
            if (!_usuarios.TryGetValue(nombreUsuario, out Usuario? usuario))
                throw new DominioException($"No existe el usuario {nombreUsuario}");

            return usuario;
        }
    }

    public bool Existe(string nombreUsuario)
    {
        lock (_candado)
            return _usuarios.ContainsKey(nombreUsuario);
    }
}
