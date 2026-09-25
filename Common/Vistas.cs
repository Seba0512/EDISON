namespace Common;

public readonly record struct Credenciales(string NombreUsuario, string Clave);

public readonly record struct DatosDeAltaDeModelo(
    string Nombre, string Descripcion, decimal Precio, DateTime FechaEntregaPrometida, int Stock);

public readonly record struct FiltrosDeBusqueda(
    string? Texto,
    decimal? PrecioMinimo,
    decimal? PrecioMaximo,
    DateTime? FechaEntregaMaxima,
    int? MinimoDisponibles);

public readonly record struct ModeloListado(
    string Nombre, decimal Precio, DateTime FechaEntregaPrometida, int UnidadesDisponibles);

public readonly record struct DetalleDeModelo(
    string Nombre,
    string Descripcion,
    decimal Precio,
    DateTime FechaEntregaPrometida,
    int UnidadesDisponibles,
    int UnidadesReservadas,
    string NombreArchivoImagen);

public readonly record struct ReservaListada(
    Guid Id, string NombreModelo, string Estado, DateTime FechaCreacion, DateTime UltimaActualizacion);

public readonly record struct ActividadListada(string Tipo, string Detalle, DateTime FechaHora);

// El estado de una reserva viaja como el texto del enum del dominio.
public static class EstadosDeReserva
{
    public const string Pendiente = "Pendiente";
    public const string Confirmada = "Confirmada";
    public const string Cancelada = "Cancelada";
    public const string Rechazada = "Rechazada";

    public static bool SePuedeCancelar(string estado)
    {
        return estado is Pendiente or Confirmada;
    }
}
