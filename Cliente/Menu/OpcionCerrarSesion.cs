using Common.Protocolo;

namespace Cliente.Menu;

internal sealed class OpcionCerrarSesion : IOpcionDeMenu
{
    private readonly ConexionConServidor _conexion;
    private readonly EstadoDelMenu _estado;

    public OpcionCerrarSesion(ConexionConServidor conexion, EstadoDelMenu estado)
    {
        _conexion = conexion;
        _estado = estado;
    }

    public string Titulo => "Cerrar sesión";

    public void Ejecutar()
    {
        RespuestaDeOperacion respuesta = _conexion.CerrarSesion();
        Console.WriteLine(respuesta.Contenido);
        if (respuesta.EsExitosa)
            _estado.RegistrarCierreDeSesion();
    }
}
