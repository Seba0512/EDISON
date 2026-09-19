using Common;
using Server.Domain.Exceptions;

namespace Server.Commands;

// Elige el handler por número de CMD.
// Agregar un comando es sumar un handler, sin tocar este código.
internal sealed class DespachadorDeComandos
{
    private readonly Dictionary<int, IManejadorDeComando> _manejadores;

    public DespachadorDeComandos(IEnumerable<IManejadorDeComando> manejadores)
    {
        _manejadores = manejadores.ToDictionary(manejador => manejador.Comando);
    }

    public RespuestaDelServidor Ejecutar(Trama solicitud, ContextoDeConexion conexion)
    {
        try
        {
            return Despachar(solicitud, conexion);
        }
        catch (DominioException excepcion)
        {
            return RespuestaDelServidor.Rechazo(excepcion.Message);
        }
        catch (ProtocoloException excepcion)
        {
            // La trama ya se consumió entera, así que un payload inválido no desincroniza la conexión.
            return RespuestaDelServidor.Rechazo(excepcion.Message);
        }
        catch (AlmacenDeImagenesException excepcion)
        {
            // Un problema de disco no desincroniza el stream, así que se rechaza y la conexión sigue.
            return RespuestaDelServidor.Rechazo(excepcion.Message);
        }
    }

    private RespuestaDelServidor Despachar(Trama solicitud, ContextoDeConexion conexion)
    {
        if (!_manejadores.TryGetValue(solicitud.Comando, out IManejadorDeComando? manejador))
        {
            return RespuestaDelServidor.Rechazo("CMD no soportado");
        }

        return manejador.Ejecutar(solicitud, conexion);
    }
}
