using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

/// <summary>
/// CMD 13: opción de menú de CR7. Pide los cinco filtros opcionales, delega la solicitud remota en
/// <see cref="BusquedaDeModelosRemota"/> y muestra el resultado con <see cref="PresentadorDeModelos"/>;
/// no conoce el formato del protocolo ni el socket subyacente.
/// </summary>
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
