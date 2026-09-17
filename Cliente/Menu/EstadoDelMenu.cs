namespace Cliente.Menu;

internal sealed class EstadoDelMenu
{
    private MenuDeOpciones? _menuAnonimo;
    private MenuDeOpciones? _menuAutenticado;
    private string _nombreUsuarioAutenticado = string.Empty;

    public bool SeguirEjecutando { get; private set; } = true;
    public MenuDeOpciones MenuActual { get; private set; } = null!;
    public string NombreUsuarioAutenticado
        => _nombreUsuarioAutenticado.Length > 0
            ? _nombreUsuarioAutenticado
            : throw new InvalidOperationException("No hay un usuario autenticado");

    public void Inicializar(MenuDeOpciones menuAnonimo, MenuDeOpciones menuAutenticado)
    {
        _menuAnonimo = menuAnonimo;
        _menuAutenticado = menuAutenticado;
        MenuActual = menuAnonimo;
    }

    public void IrAAutenticado(string nombreUsuario)
    {
        _nombreUsuarioAutenticado = nombreUsuario;
        MenuActual = ObtenerMenu(_menuAutenticado);
    }

    public void IrAAnonimo()
    {
        _nombreUsuarioAutenticado = string.Empty;
        MenuActual = ObtenerMenu(_menuAnonimo);
    }

    public void Terminar()
        => SeguirEjecutando = false;

    private static MenuDeOpciones ObtenerMenu(MenuDeOpciones? menu)
        => menu ?? throw new InvalidOperationException("EstadoDelMenu no fue inicializado");
}
