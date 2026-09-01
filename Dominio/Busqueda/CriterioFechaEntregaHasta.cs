namespace Dominio.Busqueda;

/// <summary>Filtra los modelos que se entregan a mas tardar en la fecha pedida (CR7).</summary>
public class CriterioFechaEntregaHasta : ICriterioBusqueda
{
    private readonly DateTime fechaLimite;

    public CriterioFechaEntregaHasta(DateTime fechaLimite)
    {
        this.fechaLimite = fechaLimite;
    }

    public bool Cumple(Modelo modelo)
    {
        return modelo.FechaEntregaPrometida <= fechaLimite;
    }
}
