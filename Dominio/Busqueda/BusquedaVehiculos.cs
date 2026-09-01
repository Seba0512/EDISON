namespace Dominio.Busqueda;

/// <summary>
/// Combina criterios de CR7 y aplica todos juntos sobre el catalogo. Sin criterios devuelve el
/// catalogo completo, que es el "visualizar los vehiculos disponibles" de la misma CR7.
/// La busqueda no sabe que hace cada criterio: solo les pregunta, asi que sumar filtros no la toca.
/// </summary>
public class BusquedaVehiculos
{
    private readonly List<ICriterioBusqueda> criterios = new();

    public int CantidadDeCriterios => criterios.Count;

    public void AgregarCriterio(ICriterioBusqueda criterio)
    {
        criterios.Add(criterio);
    }

    public IEnumerable<Modelo> Filtrar(IEnumerable<Modelo> catalogo)
    {
        return catalogo.Where(CumpleTodosLosCriterios).ToList();
    }

    private bool CumpleTodosLosCriterios(Modelo modelo)
    {
        return criterios.TrueForAll(criterio => criterio.Cumple(modelo));
    }
}
