namespace Dominio.Excepciones;

/// <summary>
/// Error de regla de negocio. Se agrupa una sola excepcion para todo el dominio en lugar de una por
/// operacion: el borde del servidor solo necesita distinguir "el cliente pidio algo invalido" de un
/// fallo tecnico, para responder al cliente y seguir vivo.
/// </summary>
public class DominioException : Exception
{
    public DominioException(string mensaje) : base(mensaje)
    {
    }

    public DominioException(string mensaje, Exception causaOriginal) : base(mensaje, causaOriginal)
    {
    }
}
