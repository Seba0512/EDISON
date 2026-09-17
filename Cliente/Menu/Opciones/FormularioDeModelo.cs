using Cliente.Validaciones;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

// El alta y la modificación piden exactamente los mismos cuatro campos; solo cambian los
// textos y contra qué se validan la fecha y el stock. Eso es lo único que viaja como parámetro.
internal sealed class FormularioDeModelo
{
    private readonly ReglasDelFormulario _reglas;

    private FormularioDeModelo(ReglasDelFormulario reglas)
    {
        _reglas = reglas;
    }

    public static FormularioDeModelo ParaAlta()
        => new(new ReglasDelFormulario(
            "Descripción (Enter para omitir):",
            "Precio:",
            "Fecha de entrega prometida (dd-MM-yyyy):",
            "Stock:",
            ValidacionesDeModelo.FechaFutura,
            ValidacionesDeModelo.Stock));

    public static FormularioDeModelo ParaModificacion(DetalleDeModelo detalleActual)
        => new(new ReglasDelFormulario(
            "Nueva descripción (Enter para omitir):",
            "Nuevo precio:",
            "Nueva fecha de entrega prometida (dd-MM-yyyy):",
            "Nuevo stock:",
            fecha => ValidacionesDeModelo.FechaFuturaSiCambio(fecha, detalleActual.FechaEntregaPrometida),
            stock => ValidacionesDeModelo.StockDeModificacion(stock, detalleActual)));

    // Devuelve null cuando se acaba la entrada, igual que el resto de los lectores.
    public DatosDeAltaDeModelo? Completar(string nombreModelo)
    {
        (bool sinEntrada, string? descripcion) =
            LectorDeConsola.LeerTextoOpcional(_reglas.TextoDescripcion, ValidacionesDeModelo.Descripcion);
        if (sinEntrada)
            return null;

        decimal? precio = LectorDeConsola.LeerDecimal(_reglas.TextoPrecio, ValidacionesDeModelo.Precio);
        if (precio is null)
            return null;

        DateTime? fechaEntrega = LectorDeConsola.LeerFecha(_reglas.TextoFecha, _reglas.ValidarFecha);
        if (fechaEntrega is null)
            return null;

        int? stock = LectorDeConsola.LeerEntero(_reglas.TextoStock, _reglas.ValidarStock);
        if (stock is null)
            return null;

        return new DatosDeAltaDeModelo(
            nombreModelo, descripcion ?? string.Empty, precio.Value, fechaEntrega.Value, stock.Value);
    }

    private sealed record ReglasDelFormulario(
        string TextoDescripcion,
        string TextoPrecio,
        string TextoFecha,
        string TextoStock,
        Func<DateTime, string?> ValidarFecha,
        Func<int, string?> ValidarStock);
}
