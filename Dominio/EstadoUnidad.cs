namespace Dominio;

/// <summary>
/// Estados posibles de una unidad fisica. No existe un estado "Vendida": ningun requerimiento define
/// la entrega, y una reserva confirmada ya representa la compra (CR10).
/// </summary>
public enum EstadoUnidad
{
    Disponible,
    Reservada
}
