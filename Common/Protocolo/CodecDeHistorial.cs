using Common.Protocolo.Vistas;

namespace Common.Protocolo;

/// <summary>
/// Serializa el resultado de CMD 30 (CR10): una cantidad seguida de esa cantidad de
/// <see cref="ActividadListada"/>, cada una como tres campos consecutivos (tipo, detalle, fecha/hora).
/// Es el mismo esquema de lista que usan CR7 y CR9, aplicado a actividades en vez de modelos o reservas.
/// </summary>
public static class CodecDeHistorial
{
    public static byte[] CodificarResultado(IReadOnlyList<ActividadListada> actividades)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCantidad(actividades.Count);
        foreach (ActividadListada actividad in actividades)
        {
            escritor.EscribirCampo(actividad.Tipo);
            escritor.EscribirCampo(actividad.Detalle);
            escritor.EscribirFechaHora(actividad.FechaHora);
        }

        return escritor.ObtenerBytes();
    }

    public static IReadOnlyList<ActividadListada> DecodificarResultado(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        int cantidad = lector.LeerCantidad();
        List<ActividadListada> actividades = new(cantidad);
        for (int i = 0; i < cantidad; i++)
        {
            string tipo = lector.LeerCampo();
            string detalle = lector.LeerCampo();
            DateTime fechaHora = lector.LeerFechaHora();
            actividades.Add(new ActividadListada(tipo, detalle, fechaHora));
        }

        lector.ExigirFinDePayload();
        return actividades;
    }
}
