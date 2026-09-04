using Dominio.Excepciones;
using Dominio.Fachada;
using Dominio.Repositorios;
using Dominio.Tiempo;

namespace Dominio;

/// <summary>
/// Alta, ingreso y salida de usuarios (CR1) y lectura de su historial (CR10).
///
/// Responsabilidad en una frase, sin "y": administrar el acceso de los usuarios al sistema. No
/// guarda quien esta logueado: la sesion es de una conexion, no del dominio, asi que
/// <see cref="IniciarSesion"/> devuelve el usuario y el servidor lo retiene mientras dure el socket.
/// Gracias a eso el mismo usuario puede estar conectado desde dos maquinas sin que el dominio tenga
/// estado compartido que sincronizar.
/// </summary>
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
        if (usuarios.Existe(usuario.NombreUsuario))
        {
            throw new UsuarioDuplicadoException(usuario.NombreUsuario);
        }

        usuarios.Agregar(usuario);

        return usuario;
    }

    /// <summary>
    /// No distingue "no existe el usuario" de "la clave no coincide": contestar distinto permitiria
    /// averiguar que nombres estan registrados probando de a uno.
    /// </summary>
    public Usuario IniciarSesion(string nombreUsuario, string clave)
    {
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
