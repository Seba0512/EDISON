namespace Dominio;

/// <summary>
/// Actividades que registra el historial (CR10). CierreSesion y ReservaCancelada no figuran en la
/// enumeracion de la letra, pero CR1 y CR6 las generan y sin ellas el historial quedaria incompleto.
/// </summary>
public enum TipoActividad
{
    InicioSesion,
    CierreSesion,
    ModeloCreado,
    ModeloModificado,
    ModeloEliminado,
    ReservaCreada,
    ReservaCancelada,
    Compra
}
