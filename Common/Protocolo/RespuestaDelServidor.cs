using System.Text;
using Common.Excepciones;

namespace Common.Protocolo;

/// <summary>
/// Contenido de un HEADER RES: <c>[ESTADO 2 ASCII][CUERPO]</c>. En CR1 el cuerpo siempre es texto
/// UTF-8; desde CR2-CR10 un OK puede llevar campos binarios (<c>[LARGO][UTF-8]</c> repetidos), así
/// que <see cref="Cuerpo"/> se expone crudo y <see cref="Contenido"/> lo decodifica recién si alguien
/// lo pide. ER/PR/IN siguen siendo siempre texto: ninguna fábrica de error acepta otra cosa.
/// </summary>
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

    public ReadOnlyMemory<byte> Cuerpo => _cuerpo;

    /// <summary>Decodifica el cuerpo como UTF-8. Válido siempre en ER/PR/IN; en OK sólo si ese comando responde texto plano.</summary>
    public string Contenido => _contenido ??= TextoUtf8.Decodificar(_cuerpo);

    /// <summary>
    /// Acción que se ejecuta sobre el stream de la conexión recién después de que el RES salió por la
    /// red (CMD 16): un manejador no puede escribir el archivo durante <c>Responder</c> porque el RES
    /// todavía no se escribió, así que la deja pendiente acá.
    /// </summary>
    public Action<Stream>? AccionPosterior { get; private init; }

    public static RespuestaDelServidor Ok(string contenido)
        => new(CodigosDeEstado.Ok, TextoUtf8.Codificar(contenido));

    /// <summary>OK con campos binarios en el cuerpo (listas y fichas de CR2-CR10).</summary>
    public static RespuestaDelServidor Ok(byte[] cuerpo)
        => new(CodigosDeEstado.Ok, cuerpo);

    /// <summary>OK cuyo cuerpo va seguido, fuera de frame, por un archivo (CMD 16).</summary>
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
