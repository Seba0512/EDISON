using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

internal static class PresentadorDeReservas
{
    private static readonly string[] Encabezados = ["ID", "MODELO", "ESTADO", "CREACION", "ACTUALIZACION"];

    public static void Mostrar(IReadOnlyList<ReservaListada> reservas)
    {
        Consola.Info("Mis reservas:");
        if (reservas.Count == 0)
        {
            Consola.Sutil("No tenés reservas.");
            return;
        }

        List<string[]> filas = reservas.Select(AFila).ToList();
        TablaDeConsola.Mostrar(Encabezados, filas);
    }

    private static string[] AFila(ReservaListada reserva)
        =>
        [
            reserva.Id.ToString(),
            reserva.NombreModelo,
            reserva.Estado,
            reserva.FechaCreacion.ToString(),
            reserva.UltimaActualizacion.ToString()
        ];
}
