using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

/// <summary>
/// CR7: imprime en consola, en una tabla simple de ancho fijo, el resultado de una búsqueda de
/// modelos. No sabe nada de protocolo ni de sockets: sólo recibe las filas ya decodificadas.
/// </summary>
internal static class PresentadorDeModelos
{
    public static void Mostrar(IReadOnlyList<ModeloListado> modelos)
    {
        if (modelos.Count == 0)
        {
            Console.WriteLine("No se encontraron modelos.");
            return;
        }

        Console.WriteLine($"{"Nombre",-30} {"Precio",10} {"Entrega",-12} {"Disponibles",11}");
        foreach (ModeloListado modelo in modelos)
        {
            Console.WriteLine(
                $"{modelo.Nombre,-30} {modelo.Precio,10:0.00} {modelo.FechaEntregaPrometida,-12:yyyy-MM-dd} {modelo.UnidadesDisponibles,11}");
        }
    }
}
