namespace Common;

// Los archivos no viajan dentro de DATOS. La trama del comando anuncia el largo y el contenido
// sigue crudo por el stream, en partes de LargoDeParte bytes: cada lado lee y escribe una parte
// por vez, así en memoria nunca hay más que una parte, sea cual sea el tamaño del archivo.
public sealed class TransferenciaPorPartes
{
    public const int LargoDeParte = 32 * 1024;

    private readonly Stream _red;
    private readonly byte[] _parte = new byte[LargoDeParte];

    public TransferenciaPorPartes(Stream red)
    {
        _red = red;
    }

    // Con el largo ya anunciado no hay vuelta atrás: si el archivo local falla a mitad, el otro
    // lado queda esperando bytes que no van a llegar y la conexión ya no se puede usar.
    public void Enviar(Stream archivo, long largo)
    {
        long restante = largo;
        while (restante > 0)
        {
            int largoDeParte = LargoDeLaSiguienteParte(restante);
            archivo.ReadExactly(_parte, 0, largoDeParte);
            _red.Write(_parte, 0, largoDeParte);
            restante -= largoDeParte;
        }
    }

    // Si el archivo local falla a mitad, igual se leen las partes que faltan: la conexión queda
    // sincronizada y quien recibe puede informar el error sin cortarla.
    public void Recibir(Stream archivo, long largo)
    {
        long restante = largo;
        while (restante > 0)
        {
            int largoDeParte = RecibirParte(restante);
            restante -= largoDeParte;
            try
            {
                archivo.Write(_parte, 0, largoDeParte);
            }
            catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
            {
                Descartar(restante);
                throw new ArchivoLocalException(excepcion);
            }
        }
    }

    private void Descartar(long largo)
    {
        long restante = largo;
        while (restante > 0)
        {
            restante -= RecibirParte(restante);
        }
    }

    // Read puede devolver menos bytes de los pedidos, así que se insiste hasta completar la parte.
    private int RecibirParte(long restante)
    {
        int largoDeParte = LargoDeLaSiguienteParte(restante);
        int offset = 0;
        while (offset < largoDeParte)
        {
            int recibidos = _red.Read(_parte, offset, largoDeParte - offset);
            if (recibidos == 0)
            {
                throw new TramaTruncadaException("ARCHIVO");
            }

            offset += recibidos;
        }

        return largoDeParte;
    }

    private static int LargoDeLaSiguienteParte(long restante)
    {
        return (int)Math.Min(LargoDeParte, restante);
    }
}
