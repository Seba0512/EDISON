namespace Dominio.Busqueda;

public class CriterioCompuesto : ICriterioDeBusqueda
{
    private readonly List<ICriterioDeBusqueda> criterios = new();

    public int CantidadDeCriterios => criterios.Count;

    public void Agregar(ICriterioDeBusqueda criterio)
    {
        criterios.Add(criterio);
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return criterios.TrueForAll(criterio => criterio.Cumple(vehiculo));
    }
}
