using Common.Excepciones;

namespace Common.Protocolo;

public static class EstadosDeRespuesta
{
    public const string Exitosa = "OK";
    public const string ErrorDeNegocio = "ER";
    public const string ErrorDeProtocolo = "PR";
    public const string ErrorInterno = "IN";
}

public sealed class RespuestaDeOperacion
{
    private const int LargoEstado = 2;

    private RespuestaDeOperacion(string estado, string contenido)
    {
        Estado = estado;
        Contenido = contenido;
    }

    public string Estado { get; }
    public string Contenido { get; }
    public bool EsExitosa => Estado == EstadosDeRespuesta.Exitosa;
    public bool EsErrorInterno => Estado == EstadosDeRespuesta.ErrorInterno;

    public static RespuestaDeOperacion Exitosa(string contenido = "")
        => new RespuestaDeOperacion(EstadosDeRespuesta.Exitosa, contenido);

    public static RespuestaDeOperacion ErrorDeNegocio(string mensaje)
        => new RespuestaDeOperacion(EstadosDeRespuesta.ErrorDeNegocio, mensaje);

    public static RespuestaDeOperacion ErrorDeProtocolo(string mensaje)
        => new RespuestaDeOperacion(EstadosDeRespuesta.ErrorDeProtocolo, mensaje);

    public static RespuestaDeOperacion ErrorInterno()
        => new RespuestaDeOperacion(EstadosDeRespuesta.ErrorInterno, "Ocurrió un error interno");

    public byte[] Codificar()
    {
        byte[] estado = TextoUtf8.Codificar(Estado);
        byte[] contenido = TextoUtf8.Codificar(Contenido);
        byte[] payload = new byte[LargoEstado + contenido.Length];
        estado.CopyTo(payload, 0);
        contenido.CopyTo(payload, LargoEstado);
        return payload;
    }

    public static RespuestaDeOperacion Decodificar(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < LargoEstado)
            throw new ProtocoloException("La respuesta no contiene un estado de 2 bytes");

        string estado = TextoUtf8.Decodificar(payload.Slice(0, LargoEstado));
        ExigirEstadoConocido(estado);
        string contenido = TextoUtf8.Decodificar(payload.Slice(LargoEstado));
        return new RespuestaDeOperacion(estado, contenido);
    }

    private static void ExigirEstadoConocido(string estado)
    {
        bool estadoConocido =
            estado == EstadosDeRespuesta.Exitosa
            || estado == EstadosDeRespuesta.ErrorDeNegocio
            || estado == EstadosDeRespuesta.ErrorDeProtocolo
            || estado == EstadosDeRespuesta.ErrorInterno;
        if (!estadoConocido)
            throw new ProtocoloException($"Estado de respuesta desconocido: '{estado}'");
    }
}
