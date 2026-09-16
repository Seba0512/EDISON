using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

internal static class PresentadorDeReservas
{
    private const string Encabezado = "ID                                   MODELO               ESTADO      CREACION             ACTUALIZACION";

    public static void Mostrar(IReadOnlyList<ReservaListada> reservas)
    {
        if (reservas.Count == 0)
        {
            Console.WriteLine("No tenés reservas.");
            return;
        }

        Console.WriteLine(Encabezado);
        foreach (ReservaListada reserva in reservas)
            Console.WriteLine(AFila(reserva));
    }

    private static string AFila(ReservaListada reserva)
        => string.Format(
            "{0,-36} {1,-20} {2,-11} {3,-20} {4,-20}",
            reserva.Id,
            reserva.NombreModelo,
            reserva.Estado,
            reserva.FechaCreacion,
            reserva.UltimaActualizacion);
}
