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
///
/// No atrapa <see cref="TransferenciaDeArchivoException"/>: un fallo a mitad de archivo desincroniza
/// el stream y debe propagarse como fatal, no traducirse a una respuesta más.
/// </summary>
internal sealed class DespachadorDeComandos
{
    private readonly IReadOnlyDictionary<int, IManejadorDeComando> _manejadores;

    public DespachadorDeComandos(IReadOnlyDictionary<int, IManejadorDeComando> manejadores)
    {
        _manejadores = manejadores;
    }

    public RespuestaDelServidor Despachar(Trama solicitud, EstadoDeSesion sesion, ArchivosDeSesion archivos)
    {
        if (!solicitud.EsSolicitud
            || !_manejadores.TryGetValue(solicitud.Comando, out IManejadorDeComando? manejador))
            return RespuestaDelServidor.Protocolo("CMD no soportado");

        return Ejecutar(manejador, new SolicitudRecibida(solicitud.Datos, sesion, archivos));
    }

    private static RespuestaDelServidor Ejecutar(IManejadorDeComando manejador, SolicitudRecibida solicitud)
    {
        try
        {
            return manejador.Responder(solicitud);
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
