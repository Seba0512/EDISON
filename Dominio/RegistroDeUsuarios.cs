using Dominio.Excepciones;
using Dominio.Fachada;
using Dominio.Repositorios;
using Dominio.Tiempo;

namespace Dominio;

public class RegistroDeUsuarios : IAutenticacion, IHistorialDeUsuario
{
    private readonly IRepositorioDeUsuarios _usuarios;
    private readonly IReloj _reloj;

    public RegistroDeUsuarios(IRepositorioDeUsuarios usuarios, IReloj reloj)
    {
        _usuarios = usuarios;
        _reloj = reloj;
    }

    public Usuario Registrar(string nombreUsuario, string clave)
    {
        Usuario usuario = new Usuario(nombreUsuario, clave);
        _usuarios.Agregar(usuario);

        return usuario;
    }

    public Usuario IniciarSesion(string nombreUsuario, string clave)
    {
        // Misma excepcion en ambos casos para no revelar que usuarios existen.
        if (!_usuarios.Existe(nombreUsuario))
        {
            throw new CredencialesInvalidasException();
        }

        Usuario usuario = _usuarios.ObtenerPorNombre(nombreUsuario);
        if (!usuario.VerificarClave(clave))
        {
            throw new CredencialesInvalidasException();
        }

        usuario.Registrar(Actividad.InicioSesion(_reloj));

        return usuario;
    }

    public void CerrarSesion(Usuario usuario)
    {
        usuario.Registrar(Actividad.CierreSesion(_reloj));
    }

    public IReadOnlyList<Actividad> HistorialDe(Usuario usuario)
    {
        return usuario.Actividades();
    }
}
