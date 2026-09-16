namespace Common.Protocolo.Vistas;

public readonly record struct ModeloListado(
    string Nombre, decimal Precio, DateTime FechaEntregaPrometida, int UnidadesDisponibles);
