namespace Dominio.Busqueda;

public class CriterioQueAceptaTodo : ICriterioDeBusqueda
{
    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return true;
    }
}
