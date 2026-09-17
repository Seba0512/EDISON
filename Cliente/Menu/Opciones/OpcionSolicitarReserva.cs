using Cliente.Operaciones;
using Cliente.Vistas;

namespace Cliente.Menu.Opciones;

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

        Consola.Exito(_solicitudDeReservaRemota.Ejecutar(nombreModelo));
    }
}
