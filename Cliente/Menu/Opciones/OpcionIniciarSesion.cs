using Common.Protocolo;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionIniciarSesion : IOpcionDeMenu
{
    private readonly ConexionConServidor _conexion;
    private readonly EstadoDelMenu _estado;

    public OpcionIniciarSesion(ConexionConServidor conexion, EstadoDelMenu estado)
    {
        _conexion = conexion;
        _estado = estado;
    }

    public string Titulo
        => "Iniciar sesión";

    public void Ejecutar()
    {
        Credenciales? credenciales = LectorDeConsola.LeerCredenciales();
        if (credenciales is null)
            return;

        Console.WriteLine(_conexion.IniciarSesion(credenciales.Value));
        _estado.IrAAutenticado();
    }
}
