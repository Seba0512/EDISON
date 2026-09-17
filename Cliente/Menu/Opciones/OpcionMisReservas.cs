using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionMisReservas : IOpcionDeMenu
{
    private readonly PresentadorDeReservas _presentador = new();
    private readonly ConsultaDeReservasRemota _consultaDeReservasRemota;

    public OpcionMisReservas(ConexionConServidor conexion)
    {
        _consultaDeReservasRemota = new ConsultaDeReservasRemota(conexion);
    }

    public string Titulo
        => "Mis reservas";

    public void Ejecutar()
    {
        IReadOnlyList<ReservaListada> reservas = _consultaDeReservasRemota.Ejecutar();
        _presentador.Mostrar(reservas);
    }
}
