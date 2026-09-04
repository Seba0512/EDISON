using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Dominio.Tests.Dobles;

/// <summary>
/// Repositorio de usuarios en memoria para las pruebas. Que los colaboradores del dominio se puedan
/// ejercitar contra un doble como este, sin base de datos ni servidor, es la evidencia de que las
/// interfaces de repositorio quedaron del lado correcto de la dependencia (DIP).
///
/// No lleva candado: la implementacion de produccion vive en el servidor y si lo lleva, pero cada
/// prueba usa su propia instancia.
/// </summary>
public class RepositorioDeUsuariosEnMemoria : IRepositorioDeUsuarios
{
    private readonly Dictionary<string, Usuario> usuarios = new(StringComparer.OrdinalIgnoreCase);

    public void Agregar(Usuario usuario)
    {
        usuarios[usuario.NombreUsuario] = usuario;
    }

    public Usuario ObtenerPorNombre(string nombreUsuario)
    {
        if (!usuarios.TryGetValue(nombreUsuario.Trim(), out Usuario? usuario))
        {
            throw new DominioException($"No existe el usuario {nombreUsuario}");
        }

        return usuario;
    }

    public bool Existe(string nombreUsuario)
    {
        return usuarios.ContainsKey(nombreUsuario.Trim());
    }
}
