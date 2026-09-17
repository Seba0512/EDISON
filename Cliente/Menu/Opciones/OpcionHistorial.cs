using Cliente.Operaciones;
using Cliente.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionHistorial : IOpcionDeMenu
{
    private readonly PresentadorDeHistorial _presentador = new();
    private readonly HistorialRemota _historialRemota;

    public OpcionHistorial(ConexionConServidor conexion)
    {
        _historialRemota = new HistorialRemota(conexion);
    }

    public string Titulo
        => "Historial";

    public void Ejecutar()
        => _presentador.Mostrar(_historialRemota.Ejecutar());
}
