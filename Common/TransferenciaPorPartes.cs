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
    public async Task EnviarAsync(Stream archivo, long largo, CancellationToken token = default)
    {
        long restante = largo;
        while (restante > 0)
        {
            int largoDeParte = LargoDeLaSiguienteParte(restante);
            await archivo.ReadExactlyAsync(_parte.AsMemory(0, largoDeParte), token);
            await _red.WriteAsync(_parte.AsMemory(0, largoDeParte), token);
            restante -= largoDeParte;
        }
    }

    // Si el archivo local falla a mitad, igual se leen las partes que faltan: la conexión queda
    // sincronizada y quien recibe puede informar el error sin cortarla.
    public async Task RecibirAsync(Stream archivo, long largo, CancellationToken token = default)
    {
        long restante = largo;
        while (restante > 0)
        {
            int largoDeParte = await RecibirParteAsync(restante, token);
            restante -= largoDeParte;
            try
            {
                await archivo.WriteAsync(_parte.AsMemory(0, largoDeParte), token);
            }
            catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
            {
                await DescartarAsync(restante, token);
                throw new ArchivoLocalException(excepcion);
            }
        }
    }

    private async Task DescartarAsync(long largo, CancellationToken token)
    {
        long restante = largo;
        while (restante > 0)
        {
            restante -= await RecibirParteAsync(restante, token);
        }
    }

    // ReadAsync puede devolver menos bytes de los pedidos, así que se insiste hasta completar la parte.
    private async Task<int> RecibirParteAsync(long restante, CancellationToken token)
    {
        int largoDeParte = LargoDeLaSiguienteParte(restante);
        int offset = 0;
        while (offset < largoDeParte)
        {
            int recibidos = await _red.ReadAsync(
                _parte.AsMemory(offset, largoDeParte - offset), token);
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
