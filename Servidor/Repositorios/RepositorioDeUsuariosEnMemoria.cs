using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Servidor.Repositorios;

internal sealed class RepositorioDeUsuariosEnMemoria : IRepositorioDeUsuarios
{
    private readonly object _candado = new object();
    private readonly Dictionary<string, Usuario> _usuarios =
        new Dictionary<string, Usuario>(StringComparer.OrdinalIgnoreCase);

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
            if (!_usuarios.TryGetValue(nombreUsuario.Trim(), out Usuario? usuario))
                throw new DominioException($"No existe el usuario {nombreUsuario}");

            return usuario;
        }
    }

    public bool Existe(string nombreUsuario)
    {
        lock (_candado)
            return _usuarios.ContainsKey(nombreUsuario.Trim());
    }
}
