using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionBuscarModelos : IOpcionDeMenu
{
    private readonly PresentadorDeModelos _presentador = new();
    private readonly BusquedaDeModelosRemota _busquedaDeModelosRemota;
    private readonly LectorDeFiltrosDeBusqueda _lectorDeFiltros = new();

    public OpcionBuscarModelos(ConexionConServidor conexion)
    {
        _busquedaDeModelosRemota = new BusquedaDeModelosRemota(conexion);
    }

    public string Titulo
        => "Buscar modelos";

    public void Ejecutar()
    {
        FiltrosDeBusqueda? filtros = _lectorDeFiltros.Leer();
        if (filtros is null)
            return;

        IReadOnlyList<ModeloListado> modelos = _busquedaDeModelosRemota.Ejecutar(filtros.Value);
        _presentador.Mostrar(modelos);
    }
}
