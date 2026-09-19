namespace Server.Domain.Entities;

public sealed record LecturaDeModelo(
    string Nombre,
    string Descripcion,
    decimal Precio,
    DateTime FechaEntregaPrometida,
    int StockTotal,
    int UnidadesDisponibles,
    int UnidadesReservadas,
    string NombreArchivoImagen)
{
    public bool TieneImagen => NombreArchivoImagen.Length > 0;

    public bool TieneUnidadDisponible => UnidadesDisponibles > 0;

    public bool SeLlama(string nombreModelo)
    {
        return Nombre.Equals(nombreModelo?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
