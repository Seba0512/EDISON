using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

/// <summary>
/// Imprime en consola el listado de <see cref="ReservaListada"/> de CR9. Muestra el id completo (no
/// truncado) porque CR6 lo va a pedir tal cual para cancelar una reserva; esta es la unica clase que
/// decide como se ve una reserva en pantalla, asi que un cambio de formato no toca
/// <see cref="Cliente.Menu.Opciones.OpcionMisReservas"/> ni <see cref="Operaciones.ConsultaDeReservasRemota"/>.
/// </summary>
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
