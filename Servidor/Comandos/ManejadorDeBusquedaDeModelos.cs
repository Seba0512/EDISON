using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Busqueda;
using Dominio.Fachada;
using Servidor.Busqueda;

namespace Servidor.Comandos;

/// <summary>
/// CMD 13: búsqueda de modelos (CR7). Exige sesión iniciada vía <see cref="ManejadorAutenticado"/>
/// porque, igual que el resto de CR2-CR10, sólo un usuario autenticado opera el catálogo. Decodifica
/// los filtros, delega en <see cref="FabricaDeCriterios"/> la composición del criterio de dominio y
/// proyecta cada <see cref="Modelo"/> encontrado a la vista angosta que viaja por protocolo.
/// </summary>
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
        IReadOnlyList<Modelo> modelos = _catalogoDeModelos.Buscar(criterio);

        List<ModeloListado> listado = new(modelos.Count);
        foreach (Modelo modelo in modelos)
            listado.Add(new ModeloListado(modelo.Nombre, modelo.Precio, modelo.FechaEntregaPrometida, modelo.UnidadesDisponibles));

        return RespuestaDelServidor.Ok(CodecDeBusquedaDeModelos.CodificarResultado(listado));
    }
}
