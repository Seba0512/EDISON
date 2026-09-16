namespace Common.Protocolo.Vistas;

/// <summary>Ficha completa de un modelo (CR8, CMD 14). <c>NombreArchivoImagen</c> es <c>null</c> si el modelo no tiene imagen.</summary>
public readonly record struct DetalleDeModelo(
    string Nombre,
    string Descripcion,
    decimal Precio,
    DateTime FechaEntregaPrometida,
    int UnidadesDisponibles,
    int UnidadesReservadas,
    string? NombreArchivoImagen);
