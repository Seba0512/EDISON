using System.Text;
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
            Console.WriteLine(
                $"{actividad.FechaHora:yyyy-MM-dd HH:mm:ss} UTC - {ConEspacios(actividad.Tipo)}: {actividad.Detalle}");
    }

    // El tipo llega como el nombre del enum en PascalCase (ej. "InicioSesion"): se separa en palabras para mostrarlo.
    private static string ConEspacios(string tipo)
    {
        StringBuilder resultado = new(tipo.Length + 4);
        for (int i = 0; i < tipo.Length; i++)
        {
            if (i > 0 && char.IsUpper(tipo[i]))
                resultado.Append(' ');
            resultado.Append(tipo[i]);
        }

        return resultado.ToString();
    }
}
