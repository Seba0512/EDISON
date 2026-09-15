using System.Text;
using Common.Excepciones;

namespace Common.Protocolo;

/// <summary>
/// Contenido de un HEADER RES (CR1): <c>[ESTADO 2 ASCII][CONTENIDO UTF-8]</c>. Las fábricas nombran
/// la intención de cada estado en vez de que los llamadores arrastren las constantes de
/// <see cref="CodigosDeEstado"/> sueltas.
/// </summary>
public sealed class RespuestaDelServidor
{
    private RespuestaDelServidor(string estado, string contenido)
    {
        Estado = estado;
        Contenido = contenido;
    }

    public string Estado { get; }
    public string Contenido { get; }

    public static RespuestaDelServidor Ok(string contenido)
        => new(CodigosDeEstado.Ok, contenido);

    public static RespuestaDelServidor Rechazo(string mensaje)
        => new(CodigosDeEstado.Error, mensaje);

    public static RespuestaDelServidor Protocolo(string mensaje)
        => new(CodigosDeEstado.Protocolo, mensaje);

    public static RespuestaDelServidor Interno(string mensaje)
        => new(CodigosDeEstado.Interno, mensaje);

    public byte[] Codificar()
    {
        byte[] estado = Encoding.ASCII.GetBytes(Estado);
        byte[] contenido = TextoUtf8.Codificar(Contenido);
        byte[] resultado = new byte[estado.Length + contenido.Length];
        estado.CopyTo(resultado, 0);
        contenido.CopyTo(resultado, estado.Length);
        return resultado;
    }

    public static RespuestaDelServidor Decodificar(ReadOnlySpan<byte> datos)
    {
        ExigirLargoMinimo(datos);
        string estado = Encoding.ASCII.GetString(datos[..CodigosDeEstado.LargoEstado]);
        string contenido = TextoUtf8.Decodificar(datos[CodigosDeEstado.LargoEstado..]);
        ExigirEstadoConocido(estado);
        return new RespuestaDelServidor(estado, contenido);
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
