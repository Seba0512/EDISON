namespace Dominio;

public sealed record LecturaDeReserva(
    Guid Id,
    string NombreModelo,
    string Estado,
    DateTime FechaCreacion,
    DateTime UltimaActualizacion)
{
    public bool EstaPendiente => Estado == Estados.Pendiente.Instancia.Nombre;

    public bool EstaConfirmada => Estado == Estados.Confirmada.Instancia.Nombre;

    public bool EstaActiva => EstaPendiente || EstaConfirmada;
}
