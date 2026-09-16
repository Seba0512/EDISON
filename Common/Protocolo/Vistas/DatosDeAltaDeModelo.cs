namespace Common.Protocolo.Vistas;

/// <summary>
/// Ficha completa recibida por red para el alta (CR2, CMD 10) y la modificación (CR3, CMD 11): son
/// el mismo conjunto de campos porque la modificación pide la ficha entera, no un delta.
/// </summary>
public readonly record struct DatosDeAltaDeModelo(
    string Nombre, string Descripcion, decimal Precio, DateTime FechaEntregaPrometida, int Stock);
