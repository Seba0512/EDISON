using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Busqueda;
using Dominio.Fachada;
using Servidor.Busqueda;

namespace Servidor.Comandos;

internal sealed class ManejadorDeBusquedaDeModelos : ManejadorAutenticado
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;

    public ManejadorDeBusquedaDeModelos(ICatalogoDeModelos catalogoDeModelos)
    {
        _catalogoDeModelos = catalogoDeModelos;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        FiltrosDeBusqueda filtros = CodecDeBusquedaDeModelos.DecodificarFiltros(solicitud.Datos.Span);
        ICriterioDeBusqueda criterio = FabricaDeCriterios.Crear(filtros);
        IReadOnlyList<LecturaDeModelo> modelos = _catalogoDeModelos.Buscar(criterio);

        IReadOnlyList<ModeloListado> listado = modelos.Select(AVista).ToList();
        return RespuestaDelServidor.Ok(CodecDeBusquedaDeModelos.CodificarResultado(listado));
    }

    private static ModeloListado AVista(LecturaDeModelo modelo)
        => new(
            modelo.Nombre,
            modelo.Precio,
            modelo.FechaEntregaPrometida,
            modelo.UnidadesDisponibles);
}
