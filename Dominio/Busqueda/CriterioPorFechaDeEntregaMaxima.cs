namespace Dominio.Busqueda;

/// <summary>Filtra los modelos que se entregan a mas tardar en la fecha pedida (CR7).</summary>
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
