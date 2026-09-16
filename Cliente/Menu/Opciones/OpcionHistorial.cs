using Cliente.Operaciones;
using Cliente.Vistas;

namespace Cliente.Menu.Opciones;

/// <summary>
/// Opción de menú de CMD 30 (CR10). Delega en <see cref="HistorialRemota"/> para hablar con el
/// servidor y en <see cref="PresentadorDeHistorial"/> para mostrar el resultado: nunca ve bytes ni
/// sockets, igual que el resto de las opciones de este menú.
/// </summary>
internal sealed class OpcionHistorial : IOpcionDeMenu
{
    private readonly HistorialRemota _historialRemota;

    public OpcionHistorial(ConexionConServidor conexion)
    {
        _historialRemota = new HistorialRemota(conexion);
    }

    public string Titulo
        => "Historial";

    public void Ejecutar()
        => PresentadorDeHistorial.Mostrar(_historialRemota.Ejecutar());
}
