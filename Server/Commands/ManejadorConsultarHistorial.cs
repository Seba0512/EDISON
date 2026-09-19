using Common;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorConsultarHistorial : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorConsultarHistorial(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.ConsultarHistorial;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        ExigirDatosVacios(solicitud);
        List<ActividadListada> listado = _edison
            .HistorialDe(conexion.Sesion.UsuarioAutenticado)
            .Select(actividad => new ActividadListada(
                actividad.Tipo.ToString(), actividad.Detalle, actividad.FechaHora))
            .ToList();

        return RespuestaDelServidor.Ok(Mensajes.DeActividades(listado));
    }
}
