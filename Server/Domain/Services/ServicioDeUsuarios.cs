using Server.Domain.Entities;
using Server.Domain.Exceptions;
using Server.Domain.Repositories;

namespace Server.Domain.Services;

public class ServicioDeUsuarios
{
    private const string CredencialesIncorrectas = "El usuario o la clave son incorrectos";

    private readonly IRepositorioDeUsuarios _usuarios;
    private readonly IReloj _reloj;
    // Hace atómico el par existe-agregar: dos registros simultáneos no duplican el nombre.
    private readonly object _candadoDeUnicidad = new();

    public ServicioDeUsuarios(IRepositorioDeUsuarios usuarios, IReloj reloj)
    {
        _usuarios = usuarios;
        _reloj = reloj;
    }

    public Usuario Registrar(string nombreUsuario, string clave)
    {
        Usuario usuario = new Usuario(nombreUsuario, clave);
        lock (_candadoDeUnicidad)
        {
            if (_usuarios.Existe(usuario.NombreUsuario))
            {
                throw new DominioException($"Ya existe un usuario llamado {usuario.NombreUsuario}");
            }

            _usuarios.Agregar(usuario);
        }

        return usuario;
    }

    public Usuario IniciarSesion(string nombreUsuario, string clave)
    {
        // Mismo error si no existe o la clave es incorrecta, para no revelar qué usuarios existen.
        if (!_usuarios.Existe(nombreUsuario))
        {
            throw new DominioException(CredencialesIncorrectas);
        }

        Usuario usuario = _usuarios.Obtener(nombreUsuario);
        if (!usuario.VerificarClave(clave))
        {
            throw new DominioException(CredencialesIncorrectas);
        }

        usuario.Registrar(Actividad.InicioSesion(_reloj));
        return usuario;
    }

    public void CerrarSesion(Usuario usuario)
    {
        usuario.Registrar(Actividad.CierreSesion(_reloj));
    }
}
