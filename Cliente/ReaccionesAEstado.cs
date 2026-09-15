using Cliente.Excepciones;
using Common.Excepciones;
using Common.Protocolo;

namespace Cliente;

/// <summary>
/// Traduce el ESTADO de una <see cref="RespuestaDelServidor"/> en el efecto que le corresponde en el
/// cliente (Strategy): OK devuelve el contenido, ER/PR lanzan la excepción recuperable que el menú
/// atrapa, e IN lanza la fatal. Sin switch sobre el estado.
/// </summary>
internal static class ReaccionesAEstado
{
    private static readonly Dictionary<string, IReaccionAEstado> Reacciones = new()
    {
        [CodigosDeEstado.Ok] = new AceptarContenido(),
        [CodigosDeEstado.Error] = new RechazarOperacion(),
        [CodigosDeEstado.Protocolo] = new RechazarOperacion(),
        [CodigosDeEstado.Interno] = new AbortarConexion()
    };

    public static string Aplicar(RespuestaDelServidor respuesta)
    {
        if (!Reacciones.TryGetValue(respuesta.Estado, out IReaccionAEstado? reaccion))
            throw new ProtocoloException($"ESTADO desconocido: '{respuesta.Estado}'");

        return reaccion.Reaccionar(respuesta.Contenido);
    }

    private interface IReaccionAEstado
    {
        string Reaccionar(string contenido);
    }

    private sealed class AceptarContenido : IReaccionAEstado
    {
        public string Reaccionar(string contenido)
            => contenido;
    }

    private sealed class RechazarOperacion : IReaccionAEstado
    {
        public string Reaccionar(string contenido)
            => throw new OperacionRechazadaException(contenido);
    }

    private sealed class AbortarConexion : IReaccionAEstado
    {
        public string Reaccionar(string contenido)
            => throw new ErrorInternoDelServidorException(contenido);
    }
}
