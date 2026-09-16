using Common.Protocolo.Vistas;

namespace Common.Protocolo;

/// <summary>Serializa <see cref="DatosDeAltaDeModelo"/> como cinco campos consecutivos (CR2, CMD 10).</summary>
public static class CodecDeAltaDeModelo
{
    public static byte[] Codificar(DatosDeAltaDeModelo datos)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCampo(datos.Nombre);
        escritor.EscribirCampo(datos.Descripcion);
        escritor.EscribirDecimal(datos.Precio);
        escritor.EscribirFecha(datos.FechaEntregaPrometida);
        escritor.EscribirCantidad(datos.Stock);
        return escritor.ObtenerBytes();
    }

    public static DatosDeAltaDeModelo Decodificar(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        string nombre = lector.LeerCampo();
        string descripcion = lector.LeerCampo();
        decimal precio = lector.LeerDecimal();
        DateTime fechaEntregaPrometida = lector.LeerFecha();
        int stock = lector.LeerCantidad();
        lector.ExigirFinDePayload();
        return new DatosDeAltaDeModelo(nombre, descripcion, precio, fechaEntregaPrometida, stock);
    }
}
