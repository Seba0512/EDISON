using Common.Protocolo.Vistas;

namespace Common.Protocolo;

public static class CodecDeHistorial
{
    private const int CamposPorActividad = 3;

    public static byte[] CodificarResultado(IReadOnlyList<ActividadListada> actividades)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirEntero(actividades.Count);
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
        int cantidad = lector.LeerCantidadDeElementos(CamposPorActividad);
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
