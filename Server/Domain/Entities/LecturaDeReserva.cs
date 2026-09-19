using Server.Domain.Enums;

namespace Server.Domain.Entities;

public sealed record LecturaDeReserva(
    Guid Id,
    string NombreModelo,
    EstadoDeReserva Estado,
    DateTime FechaCreacion,
    DateTime UltimaActualizacion)
{
    public bool EstaActiva => Estado is EstadoDeReserva.Pendiente or EstadoDeReserva.Confirmada;
}
