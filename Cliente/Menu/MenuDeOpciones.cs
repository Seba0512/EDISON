namespace Cliente.Menu;

/// <summary>Lista numerada de <see cref="IOpcionDeMenu"/>. Agregar una opción no toca esta clase.</summary>
internal sealed class MenuDeOpciones
{
    private readonly IReadOnlyList<IOpcionDeMenu> _opciones;

    public MenuDeOpciones(IReadOnlyList<IOpcionDeMenu> opciones)
    {
        _opciones = opciones;
    }

    /// <returns>Falso si la entrada llegó a fin de flujo (EOF) y no se ejecutó ninguna opción.</returns>
    public bool MostrarYEjecutar()
    {
        Mostrar();
        int? indice = LectorDeConsola.LeerOpcion(_opciones.Count);
        if (indice is null)
            return false;

        _opciones[indice.Value].Ejecutar();
        return true;
    }

    private void Mostrar()
    {
        for (int i = 0; i < _opciones.Count; i++)
            Console.WriteLine($"{i + 1}. {_opciones[i].Titulo}");
    }
}
