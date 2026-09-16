namespace Common.Protocolo.Vistas;

public readonly record struct FiltrosDeBusqueda(
    string? Texto,
    decimal? PrecioMinimo,
    decimal? PrecioMaximo,
    DateTime? FechaEntregaMaxima,
    int? MinimoDisponibles);
