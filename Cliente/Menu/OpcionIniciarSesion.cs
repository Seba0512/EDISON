using Common.Protocolo;

namespace Cliente.Menu;

internal sealed class OpcionIniciarSesion : IOpcionDeMenu
{
    private readonly ConexionConServidor _conexion;
    private readonly EstadoDelMenu _estado;

    public OpcionIniciarSesion(ConexionConServidor conexion, EstadoDelMenu estado)
    {
        _conexion = conexion;
        _estado = estado;
    }

    public string Titulo => "Iniciar sesión";

    public void Ejecutar()
    {
        string nombre = LectorDeConsola.LeerTextoObligatorio("Nombre de usuario");
        string clave = LectorDeConsola.LeerTextoObligatorio("Clave");
        RespuestaDeOperacion respuesta = _conexion.IniciarSesion(nombre, clave);
        Console.WriteLine(respuesta.Contenido);
        if (respuesta.EsExitosa)
            _estado.RegistrarInicioDeSesion();
    }
}
