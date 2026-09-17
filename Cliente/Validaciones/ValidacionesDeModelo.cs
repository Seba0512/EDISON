using Common.Protocolo.Vistas;

namespace Cliente.Validaciones;

// Replica en el Cliente las reglas de negocio que hoy solo valida el Dominio del lado del
// Servidor (ver DatosDeModelo, FichaDeModelo y Modelo). El objetivo es dar feedback inmediato
// campo a campo, sin esperar al rechazo del servidor al final del formulario. El Cliente no
// referencia el ensamblado Dominio (son procesos separados que se comunican por protocolo
// binario), así que estas constantes se duplican a propósito: el Servidor sigue siendo la
// autoridad final y quien puede desincronizar esto si cambian las reglas ahí.
internal static class ValidacionesDeModelo
{
    private const int LargoMinimoNombre = 2;
    private const int LargoMaximoNombre = 60;
    private const int LargoMaximoDescripcion = 500;

    public static string? Nombre(string texto)
        => texto.Length is < LargoMinimoNombre or > LargoMaximoNombre
            ? $"El nombre del modelo debe tener entre {LargoMinimoNombre} y {LargoMaximoNombre} caracteres"
            : null;

    public static string? Descripcion(string texto)
        => texto.Length > LargoMaximoDescripcion
            ? $"La descripción no puede superar los {LargoMaximoDescripcion} caracteres"
            : null;

    // El servidor redondea el precio a entero antes de exigir que sea mayor a cero.
    public static string? Precio(decimal valor)
        => Math.Round(valor, 0, MidpointRounding.AwayFromZero) <= 0
            ? "El precio debe ser mayor a cero"
            : null;

    public static string? FechaFutura(DateTime valor)
        => valor.Date <= DateTime.Today
            ? "La fecha de entrega debe ser futura"
            : null;

    public static string? FechaFuturaSiCambio(DateTime nueva, DateTime actual)
        => nueva.Date != actual.Date ? FechaFutura(nueva) : null;

    public static string? Stock(int valor)
        => valor <= 0 ? "El stock debe ser mayor a cero" : null;

    // La modificación reconstruye la misma FichaDeModelo que el alta, así que el stock nuevo
    // también debe ser mayor a cero, además de respetar reservas y disponibilidad actuales.
    public static string? StockDeModificacion(int nuevoStock, DetalleDeModelo detalleActual)
    {
        string? errorBase = Stock(nuevoStock);
        if (errorBase is not null)
            return errorBase;

        int stockTotalActual = detalleActual.UnidadesDisponibles + detalleActual.UnidadesReservadas;

        if (detalleActual.UnidadesDisponibles == 0 && nuevoStock <= stockTotalActual)
            return $"No se puede modificar el modelo {detalleActual.Nombre} porque no quedan unidades disponibles";

        if (nuevoStock != stockTotalActual && nuevoStock < detalleActual.UnidadesReservadas)
            return $"El stock no puede quedar por debajo de las {detalleActual.UnidadesReservadas} unidades reservadas";

        return null;
    }
}
