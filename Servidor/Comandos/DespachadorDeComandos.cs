using Common.Excepciones;
using Common.Protocolo;
using Dominio.Excepciones;
using Servidor.Red;

namespace Servidor.Comandos;

internal sealed class DespachadorDeComandos
{
    private readonly IReadOnlyDictionary<int, IManejadorDeComando> _manejadores;

    public DespachadorDeComandos(IEnumerable<IManejadorDeComando> manejadores)
    {
        ArgumentNullException.ThrowIfNull(manejadores);
        _manejadores = manejadores.ToDictionary(manejador => manejador.Comando);
    }

    public RespuestaDeOperacion Despachar(Trama solicitud, EstadoDeSesion sesion)
    {
        if (!solicitud.EsSolicitud)
            return RespuestaDeOperacion.ErrorDeProtocolo("El servidor esperaba HEADER REQ");
        if (!_manejadores.TryGetValue(solicitud.Comando, out IManejadorDeComando? manejador))
            return RespuestaDeOperacion.ErrorDeProtocolo(
                $"CMD {solicitud.Comando:D2} no está asignado");

        try
        {
            return manejador.Ejecutar(solicitud.Datos, sesion);
        }
        catch (DominioException exception)
        {
            return RespuestaDeOperacion.ErrorDeNegocio(exception.Message);
        }
        catch (ProtocoloException exception)
        {
            return RespuestaDeOperacion.ErrorDeProtocolo(exception.Message);
        }
    }
}
