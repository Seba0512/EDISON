namespace Dominio;

public sealed record LecturaDeReserva(
    Guid Id,
    string NombreModelo,
    string Estado,
    DateTime FechaCreacion,
    DateTime UltimaActualizacion)
{
    public bool EstaPendiente => Estado == "Pendiente";

    public bool EstaConfirmada => Estado == "Confirmada";
}
