namespace Cliente.Menu.Opciones;

internal sealed class OpcionSalir : IOpcionDeMenu
{
    private readonly EstadoDelMenu _estado;

    public OpcionSalir(EstadoDelMenu estado)
    {
        _estado = estado;
    }

    public string Titulo
        => "Salir";

    public void Ejecutar()
        => _estado.Terminar();
}
