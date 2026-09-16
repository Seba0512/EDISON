using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

/// <summary>
/// Imprime en consola el resultado de CMD 30 (CR10). El servidor ya devuelve las actividades en el
/// orden cronológico del alta del historial de dominio, así que acá no hace falta ordenar de nuevo.
/// </summary>
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
