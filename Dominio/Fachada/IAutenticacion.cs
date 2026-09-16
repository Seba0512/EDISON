namespace Dominio.Fachada;

public interface IAutenticacion
{
    Usuario Registrar(string nombreUsuario, string clave);

    Usuario IniciarSesion(string nombreUsuario, string clave);

    void CerrarSesion(Usuario usuario);
}
