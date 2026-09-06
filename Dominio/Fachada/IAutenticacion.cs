namespace Dominio.Fachada;

/// <summary>Alta, ingreso y salida de usuarios (CR1).</summary>
public interface IAutenticacion
{
    /// <exception cref="Excepciones.UsuarioDuplicadoException">Si el nombre ya esta tomado.</exception>
    Usuario Registrar(string nombreUsuario, string clave);

    /// <exception cref="Excepciones.CredencialesInvalidasException">Si el usuario o la clave no coinciden.</exception>
    Usuario IniciarSesion(string nombreUsuario, string clave);

    void CerrarSesion(Usuario usuario);
}
