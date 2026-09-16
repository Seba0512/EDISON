using Cliente.Excepciones;
using Common.Excepciones;
using Common.Protocolo;

namespace Cliente;

internal static class ReaccionesAEstado
{
    private static readonly Dictionary<string, IReaccionAEstado> Reacciones = new()
    {
        [CodigosDeEstado.Ok] = new AceptarCuerpo(),
        [CodigosDeEstado.Error] = new RechazarOperacion(),
        [CodigosDeEstado.Protocolo] = new RechazarOperacion(),
        [CodigosDeEstado.Interno] = new AbortarConexion()
    };

    public static string Aplicar(RespuestaDelServidor respuesta)
        => TextoUtf8.Decodificar(AplicarSobreCuerpo(respuesta).Span);

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

    // Un IN no tiene arreglo. La excepción llega hasta Main y el cliente cierra la conexión enseguida.
    private sealed class AbortarConexion : IReaccionAEstado
    {
        public ReadOnlyMemory<byte> Reaccionar(RespuestaDelServidor respuesta)
            => throw new ErrorInternoDelServidorException(respuesta.Contenido);
    }
}
