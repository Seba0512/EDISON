namespace Dominio.Excepciones;

/// <summary>
/// Alta de un usuario cuyo nombre ya existe (CR1, que exige nombre de usuario unico). Es una
/// <see cref="DominioException"/> porque es una regla de negocio: el borde del servidor responde el
/// mensaje al cliente y sigue vivo.
/// </summary>
public class UsuarioDuplicadoException : DominioException
{
    public UsuarioDuplicadoException(string nombreUsuario)
        : base($"Ya existe un usuario llamado {nombreUsuario}")
    {
    }
}
