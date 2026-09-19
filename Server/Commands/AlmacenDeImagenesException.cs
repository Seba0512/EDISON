namespace Server.Commands;

// Un problema de disco no es un error de red ni de protocolo. Con su propia excepción el
// despachador lo rechaza sin cerrar la conexión y sin confundirlo con un corte de red.
internal sealed class AlmacenDeImagenesException : Exception
{
    public AlmacenDeImagenesException(string mensaje, Exception causaOriginal)
        : base(mensaje, causaOriginal)
    {
    }
}
