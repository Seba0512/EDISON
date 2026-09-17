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

    public (bool Continuar, bool RequierePausa) MostrarYEjecutar()
    {
        Mostrar();
        int? indice = LectorDeConsola.LeerOpcion(_opciones.Count);
        if (indice is null)
            return (false, false);

        Console.WriteLine();
        IOpcionDeMenu opcion = _opciones[indice.Value];
        opcion.Ejecutar();
        return (true, opcion.RequierePausa);
    }

    private void Mostrar()
    {
        Consola.Titulo(_titulo());
        for (int i = 0; i < _opciones.Count; i++)
            Console.WriteLine($"{i + 1}. {_opciones[i].Titulo}");
        Console.WriteLine();
    }
}
