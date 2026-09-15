using Common.Protocolo;

namespace Cliente.Menu;

internal sealed class OpcionRegistrarse : IOpcionDeMenu
{
    private readonly ConexionConServidor _conexion;

    public OpcionRegistrarse(ConexionConServidor conexion)
        => _conexion = conexion;

    public string Titulo => "Registrarse";

    public void Ejecutar()
    {
        string nombre = LectorDeConsola.LeerTextoObligatorio("Nombre de usuario");
        string clave = LectorDeConsola.LeerTextoObligatorio("Clave");
        RespuestaDeOperacion respuesta = _conexion.Registrar(nombre, clave);
        Console.WriteLine(respuesta.Contenido);
    }
}
