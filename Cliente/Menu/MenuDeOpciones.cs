namespace Cliente.Menu;

internal sealed class MenuDeOpciones
{
    private readonly IReadOnlyList<IOpcionDeMenu> _opciones;

    public MenuDeOpciones(IEnumerable<IOpcionDeMenu> opciones)
        => _opciones = opciones.ToArray();

    public void Ejecutar()
    {
        Console.WriteLine();
        for (int indice = 0; indice < _opciones.Count; indice++)
            Console.WriteLine($"{indice + 1}. {_opciones[indice].Titulo}");

        int seleccion = LectorDeConsola.LeerOpcion(_opciones.Count);
        _opciones[seleccion - 1].Ejecutar();
    }
}
