using Cliente.Operaciones;
using Common.Excepciones;
using Common.Protocolo;

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
        Guid? idReserva = LeerGuid("Id de la reserva a cancelar:");
        if (idReserva is null)
            return;

        Console.WriteLine(_cancelacionDeReservaRemota.Ejecutar(idReserva.Value));
    }

    private static Guid? LeerGuid(string mensaje)
    {
        while (true)
        {
            string? texto = LectorDeConsola.LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                return FormatoDeCampos.AGuid(texto);
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }
}
