using System.Text;
using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

internal static class PresentadorDeHistorial
{
    private static readonly string[] Encabezados = ["Fecha (UTC)", "Tipo", "Detalle"];

    public static void Mostrar(IReadOnlyList<ActividadListada> actividades)
    {
        if (actividades.Count == 0)
        {
            Consola.Sutil("No hay actividades registradas.");
            return;
        }

        List<string[]> filas = actividades.Select(AFila).ToList();
        TablaDeConsola.Mostrar(Encabezados, filas);
    }

    private static string[] AFila(ActividadListada actividad)
        =>
        [
            actividad.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"),
            ConEspacios(actividad.Tipo),
            actividad.Detalle
        ];

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
