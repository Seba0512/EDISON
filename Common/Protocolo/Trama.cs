using Common.Excepciones;

namespace Common.Protocolo;

public sealed class Trama
{
    private readonly byte[] _datos;

    private Trama(MetadatosDeTrama metadatos, byte[] datos)
    {
        ValidarHeader(metadatos.Header);
        ValidarComando(metadatos.Comando);
        ArgumentNullException.ThrowIfNull(datos);
        Header = metadatos.Header;
        Comando = metadatos.Comando;
        _datos = datos.ToArray();
    }

    public string Header { get; }
    public int Comando { get; }
    public ReadOnlyMemory<byte> Datos => _datos;
    public bool EsSolicitud => Header == ConstantesDelProtocolo.Solicitud;
    public bool EsRespuesta => Header == ConstantesDelProtocolo.Respuesta;

    public static Trama CrearSolicitud(int comando, byte[] datos)
        => Crear(
            new MetadatosDeTrama(ConstantesDelProtocolo.Solicitud, comando), datos);

    public static Trama CrearRespuesta(int comando, byte[] datos)
        => Crear(
            new MetadatosDeTrama(ConstantesDelProtocolo.Respuesta, comando), datos);

    private static Trama Crear(MetadatosDeTrama metadatos, byte[] datos)
        => new(metadatos, datos);

    private static void ValidarHeader(string header)
    {
        bool valido = header is ConstantesDelProtocolo.Solicitud
            or ConstantesDelProtocolo.Respuesta;
        if (!valido)
            throw new ProtocoloException($"HEADER inválido: '{header}'");
    }

    private static void ValidarComando(int comando)
    {
        if (comando is < 0 or > 99)
            throw new ProtocoloException("CMD debe estar entre 00 y 99");
    }

    private readonly record struct MetadatosDeTrama(string Header, int Comando);
}
