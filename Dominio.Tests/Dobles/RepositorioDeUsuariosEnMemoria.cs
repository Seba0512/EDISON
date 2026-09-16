using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Dominio.Tests.Dobles;

public class RepositorioDeUsuariosEnMemoria : IRepositorioDeUsuarios
{
    private readonly object candado = new();
    private readonly Dictionary<string, Usuario> usuarios = new(StringComparer.OrdinalIgnoreCase);

    public void Agregar(Usuario usuario)
    {
        string nombre = usuario.NombreUsuario;
        lock (candado)
        {
            if (usuarios.ContainsKey(nombre))
            {
                throw new UsuarioDuplicadoException(nombre);
            }

            usuarios.Add(nombre, usuario);
        }
    }

    public Usuario ObtenerPorNombre(string nombreUsuario)
    {
        lock (candado)
        {
            if (!usuarios.TryGetValue(nombreUsuario.Trim(), out Usuario? usuario))
            {
                throw new DominioException($"No existe el usuario {nombreUsuario}");
            }

            return usuario;
        }
    }

    public bool Existe(string nombreUsuario)
    {
        lock (candado)
        {
            return usuarios.ContainsKey(nombreUsuario.Trim());
        }
    }
}
