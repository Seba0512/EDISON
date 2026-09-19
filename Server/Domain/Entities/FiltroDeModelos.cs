namespace Server.Domain.Entities;

public sealed record FiltroDeModelos(
    string? Texto,
    decimal? PrecioMinimo,
    decimal? PrecioMaximo,
    DateTime? FechaEntregaMaxima,
    int? MinimoDisponibles)
{
    public static FiltroDeModelos SinFiltros => new(null, null, null, null, null);

    // Un criterio en null no filtra: el modelo pasa si cumple todos los criterios presentes.
    public bool Admite(LecturaDeModelo modelo)
    {
        return CumpleTexto(modelo)
            && CumplePrecio(modelo)
            && CumpleFechaDeEntrega(modelo)
            && CumpleDisponibles(modelo);
    }

    private bool CumpleTexto(LecturaDeModelo modelo)
    {
        return Texto is null
            || modelo.Nombre.Contains(Texto.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private bool CumplePrecio(LecturaDeModelo modelo)
    {
        return (PrecioMinimo is not decimal minimo || modelo.Precio >= minimo)
            && (PrecioMaximo is not decimal maximo || modelo.Precio <= maximo);
    }

    private bool CumpleFechaDeEntrega(LecturaDeModelo modelo)
    {
        return FechaEntregaMaxima is not DateTime fechaLimite
            || modelo.FechaEntregaPrometida <= fechaLimite;
    }

    private bool CumpleDisponibles(LecturaDeModelo modelo)
    {
        return MinimoDisponibles is not int minimoDisponibles
            || modelo.UnidadesDisponibles >= minimoDisponibles;
    }
}
