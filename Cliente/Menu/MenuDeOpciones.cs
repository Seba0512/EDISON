using Cliente.Vistas;

namespace Cliente.Menu;

internal sealed class MenuDeOpciones
{
    private readonly Func<string> _titulo;
    private readonly IReadOnlyList<IOpcionDeMenu> _opciones;

    public MenuDeOpciones(Func<string> titulo, IReadOnlyList<IOpcionDeMenu> opciones)
    {
        _titulo = titulo;
        _opciones = opciones;
    }

    public void Mostrar()
    {
        Consola.Titulo(_titulo());
        for (int numero = 1; numero <= _opciones.Count; numero++)
            Console.WriteLine($"{numero}. {_opciones[numero - 1].Titulo}");

        Console.WriteLine();
    }

    // Devuelve null cuando se acaba la entrada; ahí el ciclo del menú termina.
    public IOpcionDeMenu? ElegirOpcion()
    {
        int? indice = LectorDeConsola.LeerOpcion(_opciones.Count);
        if (indice is null)
            return null;

        Console.WriteLine();
        return _opciones[indice.Value];
    }
}
