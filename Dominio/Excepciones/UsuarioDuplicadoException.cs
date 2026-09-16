namespace Dominio.Excepciones;

public class UsuarioDuplicadoException : DominioException
{
    public UsuarioDuplicadoException(string nombreUsuario)
        : base($"Ya existe un usuario llamado {nombreUsuario}")
    {
    }
}
