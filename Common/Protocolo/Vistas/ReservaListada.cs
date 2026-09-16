namespace Common.Protocolo.Vistas;

public readonly record struct ReservaListada(
    Guid Id, string NombreModelo, string Estado, DateTime FechaCreacion, DateTime UltimaActualizacion);
