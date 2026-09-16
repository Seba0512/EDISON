using Common.Protocolo.Vistas;

namespace Common.Protocolo;

/// <summary>
/// Serializa el resultado de CMD 22 (CR9): una cantidad seguida de esa cantidad de
/// <see cref="ReservaListada"/>, cada una con sus cinco campos en el orden fijo del contrato
/// (id, modelo, estado, creacion, actualizacion). No hace falta un codec de request: el REQ de
/// CMD 22 viaja vacio, igual que CerrarSesion, porque el usuario sale de la sesion autenticada.
/// </summary>
public static class CodecDeConsultaDeReservas
{
    public static byte[] CodificarResultado(IReadOnlyList<ReservaListada> reservas)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCantidad(reservas.Count);
        foreach (ReservaListada reserva in reservas)
            EscribirReserva(escritor, reserva);

        return escritor.ObtenerBytes();
    }

    public static IReadOnlyList<ReservaListada> DecodificarResultado(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        int cantidad = lector.LeerCantidad();
        List<ReservaListada> reservas = new(cantidad);
        for (int i = 0; i < cantidad; i++)
            reservas.Add(LeerReserva(ref lector));

        lector.ExigirFinDePayload();
        return reservas;
    }

    private static void EscribirReserva(EscritorDeCampos escritor, ReservaListada reserva)
    {
        escritor.EscribirGuid(reserva.Id);
        escritor.EscribirCampo(reserva.NombreModelo);
        escritor.EscribirCampo(reserva.Estado);
        escritor.EscribirFechaHora(reserva.FechaCreacion);
        escritor.EscribirFechaHora(reserva.UltimaActualizacion);
    }

    private static ReservaListada LeerReserva(ref LectorDeCampos lector)
    {
        Guid id = lector.LeerGuid();
        string nombreModelo = lector.LeerCampo();
        string estado = lector.LeerCampo();
        DateTime fechaCreacion = lector.LeerFechaHora();
        DateTime ultimaActualizacion = lector.LeerFechaHora();
        return new ReservaListada(id, nombreModelo, estado, fechaCreacion, ultimaActualizacion);
    }
}
