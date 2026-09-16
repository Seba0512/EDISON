using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionCancelarReserva : IOpcionDeMenu
{
    private readonly ConsultaDeReservasRemota _consulta;
    private readonly CancelacionDeReservaRemota _cancelacionDeReservaRemota;

    public OpcionCancelarReserva(ConexionConServidor conexion)
    {
        _consulta = new ConsultaDeReservasRemota(conexion);
        _cancelacionDeReservaRemota = new CancelacionDeReservaRemota(conexion);
    }

    public string Titulo
        => "Cancelar reserva";

    public void Ejecutar()
    {
        IReadOnlyList<ReservaListada> reservas = _consulta.Ejecutar();
        PresentadorDeReservas.Mostrar(reservas);
        if (reservas.Count == 0)
            return;

        Guid? idReserva = LectorDeConsola.LeerGuid("Id de la reserva a cancelar:");
        if (idReserva is null)
            return;

        Console.WriteLine(_cancelacionDeReservaRemota.Ejecutar(idReserva.Value));
    }
}
