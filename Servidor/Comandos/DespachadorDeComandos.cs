using Common.Excepciones;
using Common.Protocolo;
using Dominio.Excepciones;
using Servidor.Red;

namespace Servidor.Comandos;

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
