using Cliente.Menu;

namespace Cliente;

internal sealed class AplicacionCliente
{
    private readonly ConexionConServidor _conexion;
    private readonly EstadoDelMenu _estado = new EstadoDelMenu();

    public AplicacionCliente(ConexionConServidor conexion)
        => _conexion = conexion;

    public void Ejecutar()
    {
        while (!_estado.SalidaSolicitada)
        {
            MenuDeOpciones menu = _estado.EstaAutenticado
                ? CrearMenuAutenticado()
                : CrearMenuAnonimo();
            menu.Ejecutar();
        }
    }

    private MenuDeOpciones CrearMenuAnonimo()
        => new MenuDeOpciones(
            new IOpcionDeMenu[]
            {
                new OpcionRegistrarse(_conexion),
                new OpcionIniciarSesion(_conexion, _estado),
                new OpcionSalir(_estado)
            });

    private MenuDeOpciones CrearMenuAutenticado()
        => new MenuDeOpciones(
            new IOpcionDeMenu[]
            {
                new OpcionCerrarSesion(_conexion, _estado),
                new OpcionSalir(_estado)
            });
}
