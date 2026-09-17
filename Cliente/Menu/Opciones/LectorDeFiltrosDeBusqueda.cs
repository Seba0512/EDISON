using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class LectorDeFiltrosDeBusqueda
{
    // Enter vacío significa sin filtro. Solo el fin de la entrada cancela toda la búsqueda.
    public FiltrosDeBusqueda? Leer()
    {
        (bool sinEntrada, string? texto) =
            LectorDeConsola.LeerTextoOpcional("Texto a buscar en el nombre (Enter para omitir):");
        if (sinEntrada)
            return null;

        (bool canceloPrecioMinimo, decimal? precioMinimo) =
            LectorDeConsola.LeerDecimalOpcional("Precio mínimo (Enter para omitir):");
        if (canceloPrecioMinimo)
            return null;

        (bool canceloPrecioMaximo, decimal? precioMaximo) =
            LectorDeConsola.LeerDecimalOpcional("Precio máximo (Enter para omitir):");
        if (canceloPrecioMaximo)
            return null;

        (bool canceloFecha, DateTime? fechaEntregaMaxima) = LectorDeConsola.LeerFechaOpcional(
            "Fecha de entrega máxima, formato dd-MM-yyyy (Enter para omitir):");
        if (canceloFecha)
            return null;

        (bool canceloMinimo, int? minimoDisponibles) =
            LectorDeConsola.LeerEnteroOpcional("Mínimo de unidades disponibles (Enter para omitir):");
        if (canceloMinimo)
            return null;

        return new FiltrosDeBusqueda(
            texto, precioMinimo, precioMaximo, fechaEntregaMaxima, minimoDisponibles);
    }
}
