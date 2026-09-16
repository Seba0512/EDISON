namespace Cliente.Menu;

internal sealed class MenuDeOpciones
{
    private readonly IReadOnlyList<IOpcionDeMenu> _opciones;

    public MenuDeOpciones(IReadOnlyList<IOpcionDeMenu> opciones)
    {
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
        for (int i = 0; i < _opciones.Count; i++)
            Console.WriteLine($"{i + 1}. {_opciones[i].Titulo}");
    }
}
