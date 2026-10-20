using Servidor.Dominio.Entidades;

namespace Servidor.Dominio.Servicios;

// Puerto de salida del comprobante de una reserva aprobada. El dominio no sabe dónde se escribe.
public interface IGeneradorDeComprobantes
{
    Task GenerarAsync(LecturaDeReserva reserva, decimal precio);
}
