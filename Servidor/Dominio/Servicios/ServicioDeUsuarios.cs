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

    public Usuario Registrar(string nombreUsuario, string clave)
    {
        Usuario usuario = new Usuario(nombreUsuario, clave);
        lock (_estado.Candado)
        {
            if (_estado.Usuarios.ContainsKey(usuario.NombreUsuario))
            {
                throw new DominioException($"Ya existe un usuario llamado {usuario.NombreUsuario}");
            }

            _estado.Usuarios.Add(usuario.NombreUsuario, usuario);
        }

        return usuario;
    }

    public Usuario IniciarSesion(string nombreUsuario, string clave)
    {
        lock (_estado.Candado)
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
    }

    public void CerrarSesion(Usuario usuario)
    {
        lock (_estado.Candado)
        {
            usuario.Registrar(Actividad.CierreSesion());
        }
    }

    public IReadOnlyList<Actividad> HistorialDe(Usuario usuario)
    {
        lock (_estado.Candado)
        {
            return usuario.Actividades();
        }
    }
}
