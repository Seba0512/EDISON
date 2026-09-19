using Common;
using Server.Domain.Entities;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorBuscarModelos : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorBuscarModelos(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.BuscarModelos;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        FiltroDeModelos filtro = FiltroRecibido(solicitud);
        List<ModeloListado> listado = _edison.Buscar(filtro)
            .Select(modelo => new ModeloListado(
                modelo.Nombre,
                modelo.Precio,
                modelo.FechaEntregaPrometida,
                modelo.UnidadesDisponibles))
            .ToList();

        return RespuestaDelServidor.Ok(Mensajes.DeModelosListados(listado));
    }

    private static FiltroDeModelos FiltroRecibido(Trama solicitud)
    {
        FiltrosDeBusqueda filtros = Mensajes.AFiltros(solicitud.DatosComoTexto());
        return new FiltroDeModelos(
            filtros.Texto,
            filtros.PrecioMinimo,
            filtros.PrecioMaximo,
            filtros.FechaEntregaMaxima,
            filtros.MinimoDisponibles);
    }
}
