namespace Dominio.Excepciones;

public class CredencialesInvalidasException : DominioException
{
    public CredencialesInvalidasException()
        : base("El usuario o la clave son incorrectos")
    {
    }
}
