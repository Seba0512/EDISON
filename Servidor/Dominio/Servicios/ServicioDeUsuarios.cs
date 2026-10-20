using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;
using Servidor.Dominio.Repositorios;

namespace Servidor.Dominio.Servicios;

// Registro, autenticación e historial de usuarios.
public class ServicioDeUsuarios
{
    private readonly IRepositorioDeUsuarios _usuarios;
    private readonly IRepositorioDeActividades _actividades;
    private readonly CandadoGlobal _candado;

    public ServicioDeUsuarios(
        IRepositorioDeUsuarios usuarios,
        IRepositorioDeActividades actividades,
        CandadoGlobal candado)
    {
        _usuarios = usuarios;
        _actividades = actividades;
        _candado = candado;
    }

    public async Task<Usuario> RegistrarAsync(string nombreUsuario, string clave)
    {
        Usuario usuario = new Usuario(nombreUsuario, clave);
        await _candado.TomarAsync();
        try
        {
            if (await _usuarios.BuscarAsync(usuario.NombreUsuario) is not null)
            {
                throw new DominioException($"Ya existe un usuario llamado {usuario.NombreUsuario}");
            }

            await _usuarios.AgregarAsync(usuario);
        }
        finally
        {
            _candado.Liberar();
        }

        return usuario;
    }

    public async Task<Usuario> IniciarSesionAsync(string nombreUsuario, string clave)
    {
        await _candado.TomarAsync();
        try
        {
            Usuario? usuario = await _usuarios.BuscarAsync(nombreUsuario?.Trim() ?? string.Empty);
            // Mismo error si no existe o la clave es incorrecta, para no revelar qué usuarios existen.
            if (usuario is null || !usuario.VerificarClave(clave))
            {
                throw new DominioException("El usuario o la clave son incorrectos");
            }

            await _actividades.RegistrarAsync(usuario.NombreUsuario, Actividad.InicioSesion());
            return usuario;
        }
        finally
        {
            _candado.Liberar();
        }
    }

    public async Task CerrarSesionAsync(Usuario usuario)
    {
        await _candado.TomarAsync();
        try
        {
            await _actividades.RegistrarAsync(usuario.NombreUsuario, Actividad.CierreSesion());
        }
        finally
        {
            _candado.Liberar();
        }
    }

    public async Task<IReadOnlyList<Actividad>> HistorialDeAsync(Usuario usuario)
    {
        await _candado.TomarAsync();
        try
        {
            return await _actividades.ListarDeAsync(usuario.NombreUsuario);
        }
        finally
        {
            _candado.Liberar();
        }
    }
}
