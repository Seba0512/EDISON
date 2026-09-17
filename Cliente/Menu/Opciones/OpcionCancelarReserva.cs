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

            int indice = IndiceDe(reservas, candidato.Value);
            if (indice < 0)
            {
                Consola.Error("Ese id no corresponde a ninguna de tus reservas.");
                continue;
            }

            ReservaListada reserva = reservas[indice];
            if (reserva.Estado is not ("Pendiente" or "Confirmada"))
            {
                Consola.Error($"No se puede cancelar una reserva {reserva.Estado}.");
                continue;
            }

            return candidato;
        }
    }

    private static int IndiceDe(IReadOnlyList<ReservaListada> reservas, Guid id)
    {
        for (int i = 0; i < reservas.Count; i++)
        {
            if (reservas[i].Id == id)
                return i;
        }

        return -1;
    }
}
