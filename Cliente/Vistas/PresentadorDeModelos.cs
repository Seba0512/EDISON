using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

internal static class PresentadorDeModelos
{
    private static readonly string[] Encabezados = ["Nombre", "Precio", "Entrega", "Disponibles"];
    private static readonly bool[] AlinearADerecha = [false, true, false, true];

    public static void Mostrar(IReadOnlyList<ModeloListado> modelos)
    {
        if (modelos.Count == 0)
        {
            Consola.Sutil("No se encontraron modelos.");
            return;
        }

        List<string[]> filas = modelos.Select(AFila).ToList();
        TablaDeConsola.Mostrar(Encabezados, filas, AlinearADerecha);
    }

    private static string[] AFila(ModeloListado modelo)
        =>
        [
            modelo.Nombre,
            modelo.Precio.ToString("0.00"),
            modelo.FechaEntregaPrometida.ToString("dd-MM-yyyy"),
            modelo.UnidadesDisponibles.ToString()
        ];
}
