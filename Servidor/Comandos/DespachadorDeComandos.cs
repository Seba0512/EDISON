using Common.Excepciones;
using Common.Protocolo;
using Dominio.Excepciones;
using Servidor.Red;

namespace Servidor.Comandos;

/// <summary>
/// Enruta cada solicitud al <see cref="IManejadorDeComando"/> registrado para su CMD (sin switch) y
/// traduce las excepciones de negocio y de protocolo a la respuesta que le corresponde. Un CMD sin
/// manejador o una trama que llega con HEADER RES responde PR con el mismo CMD, para que la conexión
/// siga viva (regla del mapa: PR siempre es recuperable).
/// </summary>
internal sealed class DespachadorDeComandos
{
    private readonly IReadOnlyDictionary<int, IManejadorDeComando> _manejadores;

    public DespachadorDeComandos(IReadOnlyDictionary<int, IManejadorDeComando> manejadores)
    {
        _manejadores = manejadores;
    }

    public RespuestaDelServidor Despachar(Trama solicitud, EstadoDeSesion sesion)
    {
        if (!solicitud.EsSolicitud
            || !_manejadores.TryGetValue(solicitud.Comando, out IManejadorDeComando? manejador))
            return RespuestaDelServidor.Protocolo("CMD no soportado");

        return Ejecutar(manejador, solicitud, sesion);
    }

    private static RespuestaDelServidor Ejecutar(
        IManejadorDeComando manejador, Trama solicitud, EstadoDeSesion sesion)
    {
        try
        {
            return manejador.Responder(solicitud.Datos, sesion);
        }
        catch (DominioException exception)
        {
            return RespuestaDelServidor.Rechazo(exception.Message);
        }
        catch (ProtocoloException exception)
        {
            return RespuestaDelServidor.Protocolo(exception.Message);
        }
    }
}
