namespace Dominio.Excepciones;

public class ErrorDeAlmacenamientoDeImagenException : DominioException
{
    public ErrorDeAlmacenamientoDeImagenException(string mensaje, Exception causaOriginal)
        : base(mensaje, causaOriginal)
    {
    }
}
