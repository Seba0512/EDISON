using Cliente.Validaciones;
using Cliente.Vistas;
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
        Credenciales? credenciales = LectorDeConsola.LeerCredenciales(
            ValidacionesDeUsuario.NombreUsuario, ValidacionesDeUsuario.Clave);
        if (credenciales is null)
            return;

        Consola.Exito(_conexion.Registrar(credenciales.Value));
    }
}
