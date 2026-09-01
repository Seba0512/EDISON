namespace Dominio.Busqueda;

/// <summary>
/// Combina criterios de CR7 y los aplica todos juntos sobre el catalogo. Sin criterios devuelve el
/// catalogo completo, que es el "visualizar los vehiculos disponibles" de la misma CR7.
/// La busqueda no sabe que hace cada criterio: solo les pregunta, asi que sumar filtros no la toca
/// (OCP, sostenido por el Strategy de <see cref="ICriterioBusqueda"/>).
///
/// Tampoco sabe que existe <see cref="Modelo"/>: filtra cualquier <see cref="IVehiculoBuscable"/>
/// (DIP). El generico esta para devolver el mismo tipo que recibio, de modo que el llamador no tenga
/// que hacer downcast del resultado para volver a usar sus modelos.
/// </summary>
public class BusquedaVehiculos
{
    private readonly List<ICriterioBusqueda> criterios = new();

    public int CantidadDeCriterios => criterios.Count;

    public void AgregarCriterio(ICriterioBusqueda criterio)
    {
        criterios.Add(criterio);
    }

    public IEnumerable<TVehiculo> Filtrar<TVehiculo>(IEnumerable<TVehiculo> catalogo)
        where TVehiculo : IVehiculoBuscable
    {
        return catalogo.Where(vehiculo => CumpleTodosLosCriterios(vehiculo)).ToList();
    }

    private bool CumpleTodosLosCriterios(IVehiculoBuscable vehiculo)
    {
        return criterios.TrueForAll(criterio => criterio.Cumple(vehiculo));
    }
}
