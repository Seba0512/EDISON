namespace Dominio.Busqueda;

public class CriterioCompuesto : ICriterioDeBusqueda
{
    private readonly List<ICriterioDeBusqueda> _criterios = new();

    public int CantidadDeCriterios => _criterios.Count;

    public void Agregar(ICriterioDeBusqueda criterio)
    {
        _criterios.Add(criterio);
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return _criterios.TrueForAll(criterio => criterio.Cumple(vehiculo));
    }
}
