using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionCancelarReserva : IOpcionDeMenu
{
    private readonly PresentadorDeReservas _presentador = new();
    private readonly ConsultaDeReservasRemota _consultaDeReservasRemota;
    private readonly CancelacionDeReservaRemota _cancelacionDeReservaRemota;

    public OpcionCancelarReserva(ConexionConServidor conexion)
    {
        _consultaDeReservasRemota = new ConsultaDeReservasRemota(conexion);
        _cancelacionDeReservaRemota = new CancelacionDeReservaRemota(conexion);
    }

    public string Titulo
        => "Cancelar reserva";

    public void Ejecutar()
    {
        IReadOnlyList<ReservaListada> reservas = _consultaDeReservasRemota.Ejecutar();
        _presentador.Mostrar(reservas);
        if (reservas.Count == 0)
            return;

        Guid? idReserva = LeerIdDeReservaCancelable(reservas);
        if (idReserva is null)
            return;

        Consola.Exito(_cancelacionDeReservaRemota.Ejecutar(idReserva.Value));
    }

    // Valida contra las reservas ya mostradas: evita mandar al servidor un id que no está en la
    // lista, o el de una reserva que ya no se puede cancelar, y perder la elección por eso.
    private static Guid? LeerIdDeReservaCancelable(IReadOnlyList<ReservaListada> reservas)
    {
        while (true)
        {
            Guid? candidato = LectorDeConsola.LeerGuid("Id de la reserva a cancelar:");
            if (candidato is null)
                return null;

            ReservaListada? reserva = ReservaConId(reservas, candidato.Value);
            if (reserva is null)
            {
                Consola.Error("Ese id no corresponde a ninguna de tus reservas.");
                continue;
            }

            if (!EstadosDeReserva.SePuedeCancelar(reserva.Value.Estado))
            {
                Consola.Error($"No se puede cancelar una reserva {reserva.Value.Estado}.");
                continue;
            }

            return candidato;
        }
    }

    // ReservaListada es un record struct: un FirstOrDefault normal devolvería una reserva vacía
    // en vez de null, y "ese id no existe" nunca se detectaría. Por eso se pide el tipo nullable.
    private static ReservaListada? ReservaConId(IReadOnlyList<ReservaListada> reservas, Guid id)
        => reservas.Cast<ReservaListada?>().FirstOrDefault(reserva => reserva!.Value.Id == id);
}
