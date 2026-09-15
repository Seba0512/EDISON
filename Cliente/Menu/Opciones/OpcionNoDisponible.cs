namespace Cliente.Menu.Opciones;

/// <summary>
/// Marca en el menú una acción de CR2-CR10: todavía es PROPUESTA en el mapa de comandos, así que no
/// envía nada por la red.
/// </summary>
internal sealed class OpcionNoDisponible : IOpcionDeMenu
{
    public OpcionNoDisponible(string titulo)
    {
        Titulo = titulo;
    }

    public string Titulo { get; }

    public void Ejecutar()
        => Console.WriteLine($"'{Titulo}' todavía no está disponible.");
}
