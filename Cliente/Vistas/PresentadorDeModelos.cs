using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

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
                $"{modelo.Nombre,-30} {modelo.Precio,10:0.00} {modelo.FechaEntregaPrometida,-12:dd-MM-yyyy} {modelo.UnidadesDisponibles,11}");
        }
    }
}
