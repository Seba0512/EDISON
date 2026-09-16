namespace Cliente.Menu;

internal sealed class EstadoDelMenu
{
    private MenuDeOpciones? _menuAnonimo;
    private MenuDeOpciones? _menuAutenticado;

    public bool SeguirEjecutando { get; private set; } = true;
    public MenuDeOpciones MenuActual { get; private set; } = null!;

    public void Inicializar(MenuDeOpciones menuAnonimo, MenuDeOpciones menuAutenticado)
    {
        _menuAnonimo = menuAnonimo;
        _menuAutenticado = menuAutenticado;
        MenuActual = menuAnonimo;
    }

    public void IrAAutenticado()
        => MenuActual = ObtenerMenu(_menuAutenticado);

    public void IrAAnonimo()
        => MenuActual = ObtenerMenu(_menuAnonimo);

    public void Terminar()
        => SeguirEjecutando = false;

    private static MenuDeOpciones ObtenerMenu(MenuDeOpciones? menu)
        => menu ?? throw new InvalidOperationException("EstadoDelMenu no fue inicializado");
}
