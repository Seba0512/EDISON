namespace Common.Protocolo.Vistas;

/// <summary>
/// Filtros de búsqueda de modelos (CR7, CMD 13). Los cinco campos son opcionales: ausentes todos,
/// la búsqueda devuelve el catálogo completo.
/// </summary>
public readonly record struct FiltrosDeBusqueda(
    string? Texto,
    decimal? PrecioMinimo,
    decimal? PrecioMaximo,
    DateTime? FechaEntregaMaxima,
    int? MinimoDisponibles);
