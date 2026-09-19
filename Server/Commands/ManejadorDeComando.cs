using Common;

namespace Server.Commands;

// Template Method: el esqueleto "validar sesión → atender" es fijo y cada CMD completa Atender.
internal abstract class ManejadorDeComando : IManejadorDeComando
{
    public const string SesionRequerida = "Debe iniciar sesión para realizar esta operación";

    public abstract int Comando { get; }

    protected virtual bool ExigeSesion => true;

    public RespuestaDelServidor Ejecutar(Trama solicitud, ContextoDeConexion conexion)
    {
        if (ExigeSesion && !conexion.Sesion.TieneSesion)
        {
            return RespuestaDelServidor.Rechazo(SesionRequerida);
        }

        return Atender(solicitud, conexion);
    }

    protected abstract RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion);

    protected static void ExigirDatosVacios(Trama solicitud)
    {
        if (!solicitud.Datos.IsEmpty)
        {
            throw new ProtocoloException("DATOS debe estar vacío para este comando");
        }
    }
}
