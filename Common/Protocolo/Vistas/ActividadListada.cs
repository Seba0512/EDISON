namespace Common.Protocolo.Vistas;

/// <summary>Una entrada del historial del usuario autenticado (CR10, CMD 30).</summary>
public readonly record struct ActividadListada(string Tipo, string Detalle, DateTime FechaHora);
