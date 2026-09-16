namespace Dominio.Busqueda;

public class CriterioPorFechaDeEntregaMaxima : ICriterioDeBusqueda
{
    private readonly DateTime fechaLimite;

    public CriterioPorFechaDeEntregaMaxima(DateTime fechaLimite)
    {
        this.fechaLimite = fechaLimite;
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return vehiculo.FechaEntregaPrometida <= fechaLimite;
    }
}
