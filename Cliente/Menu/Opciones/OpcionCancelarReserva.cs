using Cliente.Operaciones;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionCancelarReserva : IOpcionDeMenu
{
    private readonly CancelacionDeReservaRemota _cancelacionDeReservaRemota;

    public OpcionCancelarReserva(ConexionConServidor conexion)
    {
        _cancelacionDeReservaRemota = new CancelacionDeReservaRemota(conexion);
    }

    public string Titulo
        => "Cancelar reserva";

    public void Ejecutar()
    {
        Guid? idReserva = LectorDeConsola.LeerGuid("Id de la reserva a cancelar:");
        if (idReserva is null)
            return;

        Console.WriteLine(_cancelacionDeReservaRemota.Ejecutar(idReserva.Value));
    }
}
