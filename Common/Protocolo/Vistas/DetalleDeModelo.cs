namespace Common.Protocolo.Vistas;

/// <summary>
/// Ficha completa de un modelo (CR8, CMD 14). <c>NombreArchivoImagen</c> es cadena vacía si el
/// modelo no tiene imagen, el mismo criterio que ya usa <c>Dominio.DatosDeModelo</c>.
/// </summary>
public readonly record struct DetalleDeModelo(
    string Nombre,
    string Descripcion,
    decimal Precio,
    DateTime FechaEntregaPrometida,
    int UnidadesDisponibles,
    int UnidadesReservadas,
    string NombreArchivoImagen);
