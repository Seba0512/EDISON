namespace Dominio.Busqueda;

public interface IVehiculoBuscable
{
    string Nombre { get; }

    decimal Precio { get; }

    DateTime FechaEntregaPrometida { get; }

    int UnidadesDisponibles { get; }
}
