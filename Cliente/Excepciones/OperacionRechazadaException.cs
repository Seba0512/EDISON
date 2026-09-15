namespace Cliente.Excepciones;

/// <summary>
/// El servidor respondió ER o PR (CR1): la operación no se realizó pero la conexión sigue viva y
/// el menú puede volver a mostrarse.
/// </summary>
public sealed class OperacionRechazadaException : Exception
{
    public OperacionRechazadaException(string mensaje) : base(mensaje)
    {
    }
}
