using Dominio.Busqueda;

namespace Dominio.Tests;

/// <summary>
/// Vehiculo minimo que cumple <see cref="IVehiculoBuscable"/>. Que un criterio se pueda probar
/// contra esto, sin datos ni stock ni creador, es la evidencia de que la interfaz quedo del tamanio
/// justo (ISP) y de que los criterios no dependen de <see cref="Dominio.Modelo"/> (DIP).
/// </summary>
public class VehiculoDePrueba : IVehiculoBuscable
{
    public string Nombre { get; init; } = "Vehiculo";

    public decimal Precio { get; init; }

    public DateTime FechaEntregaPrometida { get; init; }

    public int UnidadesDisponibles { get; init; }
}
