using System.Text;
using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

internal sealed class PresentadorDeHistorial : PresentadorDeTabla<ActividadListada>
{
    protected override IReadOnlyList<string> Encabezados { get; } = ["Fecha (UTC)", "Tipo", "Detalle"];

    protected override string MensajeSinElementos => "No hay actividades registradas.";

    protected override string[] AFila(ActividadListada actividad)
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
