using Cliente.Operaciones;

namespace Cliente.Menu.Opciones;

/// <summary>
/// CMD 20 en el menú autenticado (CR5): pide el nombre del modelo a reservar, delega en
/// <see cref="SolicitudDeReservaRemota"/> y muestra el texto de respuesta (que trae el Guid de la
/// reserva creada). Nunca ve bytes ni sockets.
/// </summary>
internal sealed class OpcionSolicitarReserva : IOpcionDeMenu
{
    private readonly SolicitudDeReservaRemota _solicitudDeReservaRemota;

    public OpcionSolicitarReserva(ConexionConServidor conexion)
    {
        _solicitudDeReservaRemota = new SolicitudDeReservaRemota(conexion);
    }

    public string Titulo
        => "Solicitar reserva";

    public void Ejecutar()
    {
        string? nombreModelo = LectorDeConsola.LeerTexto("Nombre del modelo a reservar:");
        if (nombreModelo is null)
            return;

        Console.WriteLine(_solicitudDeReservaRemota.Ejecutar(nombreModelo));
    }
}
