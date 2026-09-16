using Common.Protocolo.Vistas;
using Dominio.Busqueda;

namespace Servidor.Busqueda;

/// <summary>
/// Traduce los <see cref="FiltrosDeBusqueda"/> del protocolo (CMD 13, CR7) al
/// <see cref="ICriterioDeBusqueda"/> que espera <c>ICatalogoDeModelos.Buscar</c>, componiendo con
/// <see cref="CriterioCompuesto"/> únicamente los criterios de dominio cuyo filtro llegó presente.
/// No reimplementa ninguna regla de filtrado: sólo decide qué criterios ya existentes armar.
/// </summary>
internal static class FabricaDeCriterios
{
    public static ICriterioDeBusqueda Crear(FiltrosDeBusqueda filtros)
    {
        CriterioCompuesto compuesto = new();

        if (filtros.Texto is not null)
            compuesto.Agregar(new CriterioPorNombre(filtros.Texto));

        if (filtros.PrecioMinimo.HasValue)
            compuesto.Agregar(new CriterioPorPrecioMinimo(filtros.PrecioMinimo.Value));

        if (filtros.PrecioMaximo.HasValue)
            compuesto.Agregar(new CriterioPorPrecioMaximo(filtros.PrecioMaximo.Value));

        if (filtros.FechaEntregaMaxima.HasValue)
            compuesto.Agregar(new CriterioPorFechaDeEntregaMaxima(filtros.FechaEntregaMaxima.Value));

        if (filtros.MinimoDisponibles.HasValue)
            compuesto.Agregar(new CriterioPorUnidadesDisponibles(filtros.MinimoDisponibles.Value));

        if (compuesto.CantidadDeCriterios == 0)
            return new CriterioQueAceptaTodo();

        return compuesto;
    }
}
