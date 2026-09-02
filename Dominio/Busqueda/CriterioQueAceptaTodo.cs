namespace Dominio.Busqueda;

/// <summary>
/// Null Object del Strategy de CR7: representa "sin filtro" con un criterio real, para que ningun
/// metodo devuelva null ni el catalogo reparta ifs de "hay criterio?".
/// </summary>
public class CriterioQueAceptaTodo : ICriterioDeBusqueda
{
    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return true;
    }
}
