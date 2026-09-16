namespace Common.Protocolo.Vistas;

/// <summary>Una fila del resultado de búsqueda (CR7, CMD 13): sólo lo necesario para listar, no la ficha completa.</summary>
public readonly record struct ModeloListado(
    string Nombre, decimal Precio, DateTime FechaEntregaPrometida, int UnidadesDisponibles);
