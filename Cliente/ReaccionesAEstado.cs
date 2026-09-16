using Cliente.Excepciones;
using Common.Excepciones;
using Common.Protocolo;

namespace Cliente;

/// <summary>
/// Traduce el ESTADO de una <see cref="RespuestaDelServidor"/> en el efecto que le corresponde en el
/// cliente (Strategy): OK devuelve el cuerpo, ER/PR lanzan la excepción recuperable que el menú
/// atrapa, e IN lanza la fatal. Sin switch sobre el estado.
/// </summary>
internal static class ReaccionesAEstado
{
    private static readonly Dictionary<string, IReaccionAEstado> Reacciones = new()
    {
        [CodigosDeEstado.Ok] = new AceptarCuerpo(),
        [CodigosDeEstado.Error] = new RechazarOperacion(),
        [CodigosDeEstado.Protocolo] = new RechazarOperacion(),
        [CodigosDeEstado.Interno] = new AbortarConexion()
    };

    /// <summary>Aplica la reacción y decodifica el resultado como texto (CR1 y respuestas de texto plano de CR2-CR10).</summary>
    public static string Aplicar(RespuestaDelServidor respuesta)
        => TextoUtf8.Decodificar(AplicarSobreCuerpo(respuesta).Span);

    /// <summary>Aplica la reacción devolviendo el cuerpo sin decodificar (respuestas OK con campos binarios de CR2-CR10).</summary>
    public static ReadOnlyMemory<byte> AplicarSobreCuerpo(RespuestaDelServidor respuesta)
    {
        if (!Reacciones.TryGetValue(respuesta.Estado, out IReaccionAEstado? reaccion))
            throw new ProtocoloException($"ESTADO desconocido: '{respuesta.Estado}'");

        return reaccion.Reaccionar(respuesta);
    }

    private interface IReaccionAEstado
    {
        ReadOnlyMemory<byte> Reaccionar(RespuestaDelServidor respuesta);
    }

    private sealed class AceptarCuerpo : IReaccionAEstado
    {
        public ReadOnlyMemory<byte> Reaccionar(RespuestaDelServidor respuesta)
            => respuesta.Cuerpo;
    }

    private sealed class RechazarOperacion : IReaccionAEstado
    {
        public ReadOnlyMemory<byte> Reaccionar(RespuestaDelServidor respuesta)
            => throw new OperacionRechazadaException(respuesta.Contenido);
    }

    private sealed class AbortarConexion : IReaccionAEstado
    {
        public ReadOnlyMemory<byte> Reaccionar(RespuestaDelServidor respuesta)
            => throw new ErrorInternoDelServidorException(respuesta.Contenido);
    }
}
