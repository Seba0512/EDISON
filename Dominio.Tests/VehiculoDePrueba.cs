using Dominio.Busqueda;

namespace Dominio.Tests;

public class VehiculoDePrueba : IVehiculoBuscable
{
    public string Nombre { get; init; } = "Vehiculo";

    public decimal Precio { get; init; }

    public DateTime FechaEntregaPrometida { get; init; }

    public int UnidadesDisponibles { get; init; }
}
