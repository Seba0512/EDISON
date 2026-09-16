using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionBuscarModelos : IOpcionDeMenu
{
    private readonly BusquedaDeModelosRemota _busqueda;
    private readonly LectorDeFiltrosDeBusqueda _lectorDeFiltros = new();

    public OpcionBuscarModelos(ConexionConServidor conexion)
    {
        _busqueda = new BusquedaDeModelosRemota(conexion);
    }

    public string Titulo
        => "Buscar modelos";

    public void Ejecutar()
    {
        FiltrosDeBusqueda? filtros = _lectorDeFiltros.Leer();
        if (filtros is null)
            return;

        IReadOnlyList<ModeloListado> modelos = _busqueda.Ejecutar(filtros.Value);
        PresentadorDeModelos.Mostrar(modelos);
    }
}
