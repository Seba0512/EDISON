using Cliente.Operaciones;
using Common.Excepciones;
using Common.Protocolo;

namespace Cliente.Menu.Opciones;

/// <summary>
/// CMD 21 en el menú autenticado (CR6): pide el id de la reserva a cancelar como texto (el usuario lo
/// pega tal cual lo vio en "Mis reservas", que ya lo muestra completo), lo parsea con
/// <see cref="FormatoDeCampos.AGuid"/> con reintento si el formato es inválido, delega la cancelación
/// en <see cref="CancelacionDeReservaRemota"/> y muestra el texto de la respuesta. Nunca ve bytes ni
/// sockets.
/// </summary>
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
