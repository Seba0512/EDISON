using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;

namespace Servidor.Dominio.Servicios;

// Registro, autenticación e historial de usuarios.
public class ServicioDeUsuarios
{
    private readonly EstadoEnMemoria _estado;

    public ServicioDeUsuarios(EstadoEnMemoria estado)
    {
        _estado = estado;
    }

    public async Task<Usuario> RegistrarAsync(string nombreUsuario, string clave)
    {
        Usuario usuario = new Usuario(nombreUsuario, clave);
        await _estado.Candado.WaitAsync();
        try
        {
            if (_estado.Usuarios.ContainsKey(usuario.NombreUsuario))
            {
                throw new DominioException($"Ya existe un usuario llamado {usuario.NombreUsuario}");
            }

            _estado.Usuarios.Add(usuario.NombreUsuario, usuario);
        }
        finally
        {
            _estado.Candado.Release();
        }

        return usuario;
    }

    public async Task<Usuario> IniciarSesionAsync(string nombreUsuario, string clave)
    {
        await _estado.Candado.WaitAsync();
        try
        {
            Usuario? usuario = _estado.BuscarUsuario(nombreUsuario);
            // Mismo error si no existe o la clave es incorrecta, para no revelar qué usuarios existen.
            if (usuario is null || !usuario.VerificarClave(clave))
            {
                throw new DominioException("El usuario o la clave son incorrectos");
            }

            usuario.Registrar(Actividad.InicioSesion());
            return usuario;
        }
        finally
        {
            _estado.Candado.Release();
        }
    }

    public async Task CerrarSesionAsync(Usuario usuario)
    {
        await _estado.Candado.WaitAsync();
        try
        {
            usuario.Registrar(Actividad.CierreSesion());
        }
        finally
        {
            _estado.Candado.Release();
        }
    }

    public async Task<IReadOnlyList<Actividad>> HistorialDeAsync(Usuario usuario)
    {
        await _estado.Candado.WaitAsync();
        try
        {
            return usuario.Actividades();
        }
        finally
        {
            _estado.Candado.Release();
        }
    }
}
