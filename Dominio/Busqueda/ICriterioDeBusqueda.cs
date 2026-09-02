namespace Dominio.Busqueda;

/// <summary>
/// Strategy: cada criterio de filtrado de CR7 es un algoritmo intercambiable. Agregar un criterio
/// nuevo es agregar una clase, sin tocar la busqueda ni ningun switch por tipo de filtro (OCP).
/// </summary>
public interface ICriterioDeBusqueda
{
    bool Cumple(IVehiculoBuscable vehiculo);
}
