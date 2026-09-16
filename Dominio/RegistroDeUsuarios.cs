using Dominio.Excepciones;
using Dominio.Fachada;
using Dominio.Repositorios;
using Dominio.Tiempo;

namespace Dominio;

public class RegistroDeUsuarios : IAutenticacion, IHistorialDeUsuario
{
    private readonly IRepositorioDeUsuarios usuarios;
    private readonly IReloj reloj;

    public RegistroDeUsuarios(IRepositorioDeUsuarios usuarios, IReloj reloj)
    {
        this.usuarios = usuarios;
        this.reloj = reloj;
    }

    public Usuario Registrar(string nombreUsuario, string clave)
    {
        Usuario usuario = new Usuario(nombreUsuario, clave);
        usuarios.Agregar(usuario);

        return usuario;
    }

    public Usuario IniciarSesion(string nombreUsuario, string clave)
    {
        // Misma excepcion en ambos casos para no revelar que usuarios existen.
        if (!usuarios.Existe(nombreUsuario))
        {
            throw new CredencialesInvalidasException();
        }

        Usuario usuario = usuarios.ObtenerPorNombre(nombreUsuario);
        if (!usuario.VerificarClave(clave))
        {
            throw new CredencialesInvalidasException();
        }

        usuario.Registrar(Actividad.InicioSesion(reloj));

        return usuario;
    }

    public void CerrarSesion(Usuario usuario)
    {
        usuario.Registrar(Actividad.CierreSesion(reloj));
    }

    public IReadOnlyList<Actividad> HistorialDe(Usuario usuario)
    {
        return usuario.Actividades();
    }
}
