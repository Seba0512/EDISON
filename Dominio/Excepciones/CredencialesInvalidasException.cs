namespace Dominio.Excepciones;

/// <summary>
/// Intento de inicio de sesion con usuario o clave incorrectos (CR1).
///
/// El mensaje no distingue si fallo el nombre o la clave, y es deliberado: contestar "ese usuario no
/// existe" le permitiria a cualquiera averiguar que nombres estan registrados probando de a uno.
/// </summary>
public class CredencialesInvalidasException : DominioException
{
    public CredencialesInvalidasException()
        : base("El usuario o la clave son incorrectos")
    {
    }
}
