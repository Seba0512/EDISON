namespace Dominio.Busqueda;

public class CriterioPorFechaDeEntregaMaxima : ICriterioDeBusqueda
{
    private readonly DateTime _fechaLimite;

    public CriterioPorFechaDeEntregaMaxima(DateTime fechaLimite)
    {
        _fechaLimite = fechaLimite;
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return vehiculo.FechaEntregaPrometida <= _fechaLimite;
    }
}
