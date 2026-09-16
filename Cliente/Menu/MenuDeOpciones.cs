namespace Cliente.Menu;

internal sealed class MenuDeOpciones
{
    private readonly IReadOnlyList<IOpcionDeMenu> _opciones;

    public MenuDeOpciones(IReadOnlyList<IOpcionDeMenu> opciones)
    {
        _opciones = opciones;
    }

    public bool MostrarYEjecutar()
    {
        Mostrar();
        int? indice = LectorDeConsola.LeerOpcion(_opciones.Count);
        if (indice is null)
            return false;

        Console.WriteLine();
        _opciones[indice.Value].Ejecutar();
        return true;
    }

    private void Mostrar()
    {
        for (int i = 0; i < _opciones.Count; i++)
            Console.WriteLine($"{i + 1}. {_opciones[i].Titulo}");
    }
}
