using System.Text;
using Common.Excepciones;

namespace Common.Protocolo;

public sealed class RespuestaDelServidor
{
    private readonly byte[] _cuerpo;
    private string? _contenido;

    private RespuestaDelServidor(string estado, byte[] cuerpo)
    {
        Estado = estado;
        _cuerpo = cuerpo;
    }

    public string Estado { get; }

    // En OK el cuerpo puede ser binario. En ER, PR e IN siempre es texto UTF-8.
    public ReadOnlyMemory<byte> Cuerpo => _cuerpo;

    public string Contenido => _contenido ??= TextoUtf8.Decodificar(_cuerpo);

    // Se ejecuta recién después de mandar la respuesta. El archivo de CMD 16 viaja fuera de la trama.
    public Action<Stream>? AccionPosterior { get; private init; }

    public static RespuestaDelServidor Ok(string contenido)
        => new(CodigosDeEstado.Ok, TextoUtf8.Codificar(contenido));

    public static RespuestaDelServidor Ok(byte[] cuerpo)
        => new(CodigosDeEstado.Ok, cuerpo);

    public static RespuestaDelServidor OkConEnvioPosterior(byte[] cuerpo, Action<Stream> accionPosterior)
        => new(CodigosDeEstado.Ok, cuerpo) { AccionPosterior = accionPosterior };

    public static RespuestaDelServidor Rechazo(string mensaje)
        => new(CodigosDeEstado.Error, TextoUtf8.Codificar(mensaje));

    public static RespuestaDelServidor Protocolo(string mensaje)
        => new(CodigosDeEstado.Protocolo, TextoUtf8.Codificar(mensaje));

    public static RespuestaDelServidor Interno(string mensaje)
        => new(CodigosDeEstado.Interno, TextoUtf8.Codificar(mensaje));

    public byte[] Codificar()
    {
        byte[] estado = Encoding.ASCII.GetBytes(Estado);
        byte[] resultado = new byte[estado.Length + _cuerpo.Length];
        estado.CopyTo(resultado, 0);
        _cuerpo.CopyTo(resultado, estado.Length);
        return resultado;
    }

    public static RespuestaDelServidor Decodificar(ReadOnlySpan<byte> datos)
    {
        ExigirLargoMinimo(datos);
        string estado = Encoding.ASCII.GetString(datos[..CodigosDeEstado.LargoEstado]);
        ExigirEstadoConocido(estado);
        byte[] cuerpo = datos[CodigosDeEstado.LargoEstado..].ToArray();
        return new RespuestaDelServidor(estado, cuerpo);
    }

    private static void ExigirLargoMinimo(ReadOnlySpan<byte> datos)
    {
        if (datos.Length < CodigosDeEstado.LargoEstado)
            throw new ProtocoloException("La respuesta no contiene un ESTADO completo");
    }

    private static void ExigirEstadoConocido(string estado)
    {
        bool conocido = estado is CodigosDeEstado.Ok or CodigosDeEstado.Error
            or CodigosDeEstado.Protocolo or CodigosDeEstado.Interno;
        if (!conocido)
            throw new ProtocoloException($"ESTADO desconocido: '{estado}'");
    }
}
