using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

internal static class PresentadorDeHistorial
{
    public static void Mostrar(IReadOnlyList<ActividadListada> actividades)
    {
        if (actividades.Count == 0)
        {
            Console.WriteLine("No hay actividades registradas.");
            return;
        }

        foreach (ActividadListada actividad in actividades)
            Console.WriteLine($"{actividad.FechaHora:yyyy-MM-dd HH:mm:ss} UTC - {actividad.Tipo}: {actividad.Detalle}");
    }
}
