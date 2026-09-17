using System.Buffers.Binary;
using System.Text;
using Common.Excepciones;

namespace Common.Protocolo;

public sealed class CodecDeTramas
{
    private readonly int _maxPayloadBytes;

    public CodecDeTramas(int maxPayloadBytes)
    {
        if (maxPayloadBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxPayloadBytes));

        _maxPayloadBytes = maxPayloadBytes;
    }

    public Trama Leer(Stream stream)
    {
        ExigirLectura(stream);
        string header = LeerHeader(stream);
        int comando = LeerComando(stream);
        int largo = LeerLargo(stream);
        byte[] datos = LeerExactamente(stream, new Campo(largo, "DATOS"));
        return header == ConstantesDelProtocolo.Solicitud
            ? Trama.CrearSolicitud(comando, datos)
            : Trama.CrearRespuesta(comando, datos);
    }

    public void Escribir(Stream stream, Trama trama)
    {
        ExigirEscritura(stream);
        ArgumentNullException.ThrowIfNull(trama);
        ExigirLargoAdmitidoEnEscritura(trama.Datos.Length);

        byte[] prefijo = CrearPrefijo(trama);
        stream.Write(prefijo, 0, prefijo.Length);
        if (!trama.Datos.IsEmpty)
            stream.Write(trama.Datos.Span);
    }

    private string LeerHeader(Stream stream)
    {
        Campo campo = new(ConstantesDelProtocolo.LargoHeader, "HEADER");
        byte[] bytes = LeerExactamente(stream, campo, esInicioDeTrama: true);
        string header = Encoding.ASCII.GetString(bytes);
        if (header is not ConstantesDelProtocolo.Solicitud
            and not ConstantesDelProtocolo.Respuesta)
            throw new ProtocoloException($"HEADER inválido: '{header}'");

        return header;
    }

    private static int LeerComando(Stream stream)
    {
        Campo campo = new(ConstantesDelProtocolo.LargoComando, "CMD");
        byte[] bytes = LeerExactamente(stream, campo);
        if (!EsDigito(bytes[0]) || !EsDigito(bytes[1]))
            throw new ProtocoloException("CMD no contiene dos dígitos ASCII");

        return (bytes[0] - (byte)'0') * 10 + bytes[1] - (byte)'0';
    }

    private int LeerLargo(Stream stream)
    {
        Campo campo = new(ConstantesDelProtocolo.LargoCampoLargo, "LARGO");
        byte[] bytes = LeerExactamente(stream, campo);
        // LARGO viaja como número de 4 bytes y no como texto ASCII, a diferencia de HEADER y CMD.
        int largo = BinaryPrimitives.ReadInt32BigEndian(bytes);
        ExigirLargoAdmitido(largo);
        return largo;
    }

    private static byte[] LeerExactamente(Stream stream, Campo campo, bool esInicioDeTrama = false)
    {
        byte[] buffer = new byte[campo.Largo];
        int offset = 0;
        // Read puede devolver menos bytes de los pedidos, así que se insiste hasta completar el campo.
        while (offset < campo.Largo)
        {
            int recibidos = stream.Read(buffer, offset, campo.Largo - offset);
            if (recibidos == 0)
            {
                // Cero bytes antes de empezar la trama es un cierre ordenado del otro lado.
                // Cero bytes a mitad de camino significa que la trama quedó cortada.
                throw esInicioDeTrama && offset == 0
                    ? new ConexionCerradaException()
                    : new TramaTruncadaException(campo.Nombre);
            }

            offset += recibidos;
        }

        return buffer;
    }

    private byte[] CrearPrefijo(Trama trama)
    {
        byte[] prefijo = new byte[ConstantesDelProtocolo.LargoPrefijo];
        Encoding.ASCII.GetBytes(
            trama.Header,
            prefijo.AsSpan(
                ConstantesDelProtocolo.OffsetHeader,
                ConstantesDelProtocolo.LargoHeader));
        EscribirComando(prefijo, trama.Comando);
        BinaryPrimitives.WriteInt32BigEndian(
            prefijo.AsSpan(
                ConstantesDelProtocolo.OffsetLargo,
                ConstantesDelProtocolo.LargoCampoLargo),
            trama.Datos.Length);
        return prefijo;
    }

    private static void EscribirComando(byte[] prefijo, int comando)
    {
        prefijo[ConstantesDelProtocolo.OffsetDecenasComando] =
            (byte)('0' + comando / 10);
        prefijo[ConstantesDelProtocolo.OffsetUnidadesComando] =
            (byte)('0' + comando % 10);
    }

    private void ExigirLargoAdmitido(int largo)
        => ExigirLargoAdmitido(largo, mensaje => new ProtocoloException(mensaje));

    // Al escribir (por ejemplo, una respuesta de historial/búsqueda demasiado grande) nada se envió
    // aún, así que quien llama puede recuperarse enviando un error en su lugar en vez de cortar la
    // conexión. Por eso el exceso al escribir usa una excepción propia, distinguible de la de lectura.
    private void ExigirLargoAdmitidoEnEscritura(int largo)
        => ExigirLargoAdmitido(largo, mensaje => new PayloadDemasiadoGrandeException(mensaje));

    private void ExigirLargoAdmitido(int largo, Func<string, ProtocoloException> comoExcepcion)
    {
        if (largo < 0)
            throw new ProtocoloException("LARGO no puede ser negativo");
        if (largo > _maxPayloadBytes)
            throw comoExcepcion($"LARGO {largo} supera el máximo admitido {_maxPayloadBytes}");
    }

    private static bool EsDigito(byte valor)
        => valor is >= (byte)'0' and <= (byte)'9';

    private static void ExigirLectura(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
            throw new ArgumentException("El stream no permite lectura", nameof(stream));
    }

    private static void ExigirEscritura(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite)
            throw new ArgumentException("El stream no permite escritura", nameof(stream));
    }

    private readonly record struct Campo(int Largo, string Nombre);
}
