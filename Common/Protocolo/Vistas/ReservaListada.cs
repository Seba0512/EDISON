namespace Common.Protocolo.Vistas;

/// <summary>Una reserva del usuario autenticado (CR9, CMD 22).</summary>
public readonly record struct ReservaListada(
    Guid Id, string NombreModelo, string Estado, DateTime FechaCreacion, DateTime UltimaActualizacion);
