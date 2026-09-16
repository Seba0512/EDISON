using System.Text;
using Common.Excepciones;

namespace Common.Protocolo;

public static class TextoUtf8
{
    // UTF-8 estricto. Un byte inválido tiene que dar error y no convertirse en un carácter de relleno.
    private static readonly UTF8Encoding Encoding = new(false, true);

    public static byte[] Codificar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        try
        {
            return Encoding.GetBytes(texto);
        }
        catch (EncoderFallbackException exception)
        {
            throw new ProtocoloException("El texto no es UTF-8 válido", exception);
        }
    }

    public static string Decodificar(ReadOnlySpan<byte> bytes)
    {
        try
        {
            return Encoding.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new ProtocoloException("DATOS no contiene UTF-8 válido", exception);
        }
    }
}
