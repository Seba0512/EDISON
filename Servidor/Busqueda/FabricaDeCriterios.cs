using Common.Protocolo.Vistas;
using Dominio.Busqueda;

namespace Servidor.Busqueda;

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

        // Sin ningún filtro explícito solo se muestran modelos con unidades disponibles.
        if (compuesto.CantidadDeCriterios == 0)
            return new CriterioPorUnidadesDisponibles(1);

        return compuesto;
    }
}
