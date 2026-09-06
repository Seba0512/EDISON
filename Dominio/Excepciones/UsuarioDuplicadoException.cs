namespace Dominio.Excepciones;

/// <summary>
/// Alta de un usuario cuyo nombre ya existe (CR1, que exige nombre de usuario unico). Es una
/// <see cref="DominioException"/> porque representa una regla de negocio incumplida.
/// </summary>
public class UsuarioDuplicadoException : DominioException
{
    public UsuarioDuplicadoException(string nombreUsuario)
        : base($"Ya existe un usuario llamado {nombreUsuario}")
    {
    }
}
