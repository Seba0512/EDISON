using Common.Protocolo.Vistas;

namespace Common.Protocolo;

/// <summary>
/// Serializa <see cref="DatosDeAltaDeModelo"/> como cinco campos consecutivos (CR3, CMD 11). El
/// payload es identico al de CMD 10 (la ficha completa: la modificacion no pide un delta, sino que
/// vuelve a mandar todos los datos), pero se define un codec propio, sin depender de
/// <see cref="CodecDeAltaDeModelo"/>, porque son dos comandos distintos que evolucionan por separado.
/// </summary>
public static class CodecDeModificacionDeModelo
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
