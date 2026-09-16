using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionMisReservas : IOpcionDeMenu
{
    private readonly ConsultaDeReservasRemota _consulta;

    public OpcionMisReservas(ConexionConServidor conexion)
    {
        _consulta = new ConsultaDeReservasRemota(conexion);
    }

    public string Titulo
        => "Mis reservas";

    public void Ejecutar()
    {
        IReadOnlyList<ReservaListada> reservas = _consulta.Ejecutar();
        PresentadorDeReservas.Mostrar(reservas);
    }
}
