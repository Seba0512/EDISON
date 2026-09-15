using Common.Protocolo;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionRegistrarse : IOpcionDeMenu
{
    private readonly ConexionConServidor _conexion;

    public OpcionRegistrarse(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public string Titulo
        => "Registrarse";

    public void Ejecutar()
    {
        Credenciales? credenciales = LectorDeConsola.LeerCredenciales();
        if (credenciales is null)
            return;

        Console.WriteLine(_conexion.Registrar(credenciales.Value));
    }
}
