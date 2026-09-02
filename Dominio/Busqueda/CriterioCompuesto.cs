namespace Dominio.Busqueda;

/// <summary>
/// Combina criterios de CR7: un vehiculo entra en el resultado solo si cumple todos. Sin criterios
/// acepta todo, que es el "visualizar los vehiculos disponibles" de la misma CR7.
///
/// Es un criterio mas (Composite sobre el Strategy): quien filtra el catalogo recibe un unico
/// <see cref="ICriterioDeBusqueda"/> y no sabe si adentro hay uno, cinco o ninguno. Sumar filtros
/// no toca a nadie (OCP).
/// </summary>
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
