namespace Dominio.Busqueda;

public interface ICriterioDeBusqueda
{
    bool Cumple(IVehiculoBuscable vehiculo);
}
