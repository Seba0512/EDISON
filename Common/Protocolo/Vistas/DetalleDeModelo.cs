namespace Common.Protocolo.Vistas;

public readonly record struct DetalleDeModelo(
    string Nombre,
    string Descripcion,
    decimal Precio,
    DateTime FechaEntregaPrometida,
    int UnidadesDisponibles,
    int UnidadesReservadas,
    string NombreArchivoImagen);
