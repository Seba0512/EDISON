using Server.Domain.Entities;
using Server.Domain.Exceptions;

namespace Server.Domain.Repositories;

public class RepositorioDeUsuariosEnMemoria : IRepositorioDeUsuarios
{
    private readonly Dictionary<string, Usuario> _usuarios = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _candado = new();

    public void Agregar(Usuario usuario)
    {
        lock (_candado)
        {
            _usuarios.Add(usuario.NombreUsuario, usuario);
        }
    }

    public bool Existe(string nombreUsuario)
    {
        lock (_candado)
        {
            return _usuarios.ContainsKey(ClaveDe(nombreUsuario));
        }
    }

    public Usuario Obtener(string nombreUsuario)
    {
        lock (_candado)
        {
            string clave = ClaveDe(nombreUsuario);
            if (!_usuarios.TryGetValue(clave, out Usuario? usuario))
            {
                throw new DominioException($"No existe el usuario {clave}");
            }

            return usuario;
        }
    }

    private static string ClaveDe(string nombreUsuario)
    {
        return nombreUsuario?.Trim() ?? string.Empty;
    }
}
