using Common.Protocolo.Vistas;

namespace Common.Protocolo;

public static class CodecDeConsultaDeModelo
{
    public static byte[] CodificarSolicitud(string nombreModelo)
        => CodecDeCampoUnico.Codificar(nombreModelo);

    public static string DecodificarSolicitud(ReadOnlySpan<byte> datos)
        => CodecDeCampoUnico.Decodificar(datos);

    public static byte[] CodificarResultado(DetalleDeModelo detalle)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCampo(detalle.Nombre);
        escritor.EscribirCampo(detalle.Descripcion);
        escritor.EscribirDecimal(detalle.Precio);
        escritor.EscribirFecha(detalle.FechaEntregaPrometida);
        escritor.EscribirEntero(detalle.UnidadesDisponibles);
        escritor.EscribirEntero(detalle.UnidadesReservadas);
        escritor.EscribirCampo(detalle.NombreArchivoImagen);
        return escritor.ObtenerBytes();
    }

    public static DetalleDeModelo DecodificarResultado(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        string nombre = lector.LeerCampo();
        string descripcion = lector.LeerCampo();
        decimal precio = lector.LeerDecimal();
        DateTime fechaEntregaPrometida = lector.LeerFecha();
        int unidadesDisponibles = lector.LeerEntero();
        int unidadesReservadas = lector.LeerEntero();
        string nombreArchivoImagen = lector.LeerCampo();
        lector.ExigirFinDePayload();

        return new DetalleDeModelo(
            nombre,
            descripcion,
            precio,
            fechaEntregaPrometida,
            unidadesDisponibles,
            unidadesReservadas,
            nombreArchivoImagen);
    }
}
