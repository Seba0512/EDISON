namespace Common.Excepciones;

public sealed class ConexionCerradaException : IOException
{
    public ConexionCerradaException()
        : base("El extremo remoto cerró la conexión entre tramas")
    {
    }
}
