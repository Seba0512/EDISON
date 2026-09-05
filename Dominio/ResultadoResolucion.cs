namespace Dominio;

/// <summary>
/// Resultado de una invocacion a resolver una reserva. Describe la transicion producida por esa
/// llamada, no el estado que la reserva ya tenia antes de recibirla.
/// </summary>
public enum ResultadoResolucion
{
    SinCambios,
    Confirmada,
    Rechazada
}
