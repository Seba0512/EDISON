namespace Common.Protocolo.Vistas;

public readonly record struct DatosDeAltaDeModelo(
    string Nombre, string Descripcion, decimal Precio, DateTime FechaEntregaPrometida, int Stock);
