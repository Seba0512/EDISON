namespace Dominio;

/// <summary>
/// Estados de una solicitud de reserva (CR5, CR6). Rechazada y Cancelada son terminales.
/// </summary>
public enum EstadoReserva
{
    Pendiente,
    Confirmada,
    Rechazada,
    Cancelada
}
