namespace Server.Domain.Exceptions;

public class DominioException : Exception
{
    public DominioException(string mensaje) : base(mensaje)
    {
    }

    public DominioException(string mensaje, Exception causaOriginal) : base(mensaje, causaOriginal)
    {
    }
}
