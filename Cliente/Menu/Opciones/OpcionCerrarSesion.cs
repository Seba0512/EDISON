using Cliente.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionCerrarSesion : IOpcionDeMenu
{
    private readonly ConexionConServidor _conexion;
    private readonly EstadoDelMenu _estado;

    public OpcionCerrarSesion(ConexionConServidor conexion, EstadoDelMenu estado)
    {
        _conexion = conexion;
        _estado = estado;
    }

    public string Titulo
        => "Cerrar sesión";

    public void Ejecutar()
    {
        Consola.Exito(_conexion.CerrarSesion());
        _estado.IrAAnonimo();
    }
}
