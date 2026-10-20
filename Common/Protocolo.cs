using System.Buffers.Binary;
using System.Text;

namespace Common;

public static class Comandos
{
    public const int Registrar = 1;
    public const int IniciarSesion = 2;
    public const int CerrarSesion = 3;

    public const int AltaDeModelo = 10;
    public const int ModificarModelo = 11;
    public const int BajaDeModelo = 12;
    public const int BuscarModelos = 13;
    public const int ConsultarModelo = 14;
    public const int SubirImagen = 15;
    public const int DescargarImagen = 16;

    public const int SolicitarReserva = 20;
    public const int CancelarReserva = 21;
    public const int ConsultarMisReservas = 22;

    public const int ConsultarHistorial = 30;
}

public static class Estados
{
    public const string Ok = "OK";
    // Tras un ER la conexión sigue funcionando. Tras un IN el cliente la cierra.
    public const string Error = "ER";
    public const string Interno = "IN";
    public const int Largo = 2;
}

// Los límites son reglas del protocolo, no del entorno: cliente y servidor tienen que usar los
// mismos valores, así que se definen una sola vez acá en vez de configurarse en cada uno.
public static class Limites
{
    public const long LargoMaximoDeArchivo = 10 * 1024 * 1024;

    // Tope de DATOS: frena un LARGO absurdo antes de reservar memoria para leerlo. Los archivos
    // no viajan en DATOS sino después de la trama, en partes (TransferenciaPorPartes), así que
    // DATOS solo lleva texto: 1 MiB alcanza para listados de miles de modelos.
    public const int LargoMaximoDeDatos = 1024 * 1024;
}

// La trama es [CMD: 2 caracteres '00'-'99'][LARGO: Int32 big-endian][DATOS: LARGO bytes].
// No lleva REQ/RES porque el cliente solo envía solicitudes y el servidor solo respuestas.
public sealed class Trama
{
    private readonly byte[] _datos;

    public Trama(int comando, byte[] datos)
    {
        if (comando is < 0 or > 99)
        {
            throw new ProtocoloException("CMD debe estar entre 00 y 99");
        }

        ArgumentNullException.ThrowIfNull(datos);
        Comando = comando;
        _datos = datos;
    }

    public int Comando { get; }

    public ReadOnlyMemory<byte> Datos => _datos;

    public string DatosComoTexto()
    {
        return TextoUtf8.Decodificar(_datos);
    }
}

public static class TextoUtf8
{
    // UTF-8 estricto. Un byte inválido tiene que dar error y no convertirse en un carácter de relleno.
    private static readonly Encoding Codificacion = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    // Un texto con medio carácter (un surrogate suelto) no tiene representación UTF-8: igual que al
    // decodificar, se informa como error de protocolo y no como una excepción de .NET sin manejar.
    public static byte[] Codificar(string texto)
    {
        try
        {
            return Codificacion.GetBytes(texto);
        }
        catch (EncoderFallbackException excepcion)
        {
            throw new ProtocoloException("El texto contiene caracteres que no se pueden enviar", excepcion);
        }
    }

    public static string Decodificar(ReadOnlySpan<byte> bytes)
    {
        try
        {
            return Codificacion.GetString(bytes);
        }
        catch (DecoderFallbackException excepcion)
        {
            throw new ProtocoloException("El texto recibido no es UTF-8 válido", excepcion);
        }
    }
}

public sealed class CodecDeTramas
{
    private const int LargoComando = 2;
    private const int LargoCampoLargo = 4;

    public async Task<Trama> LeerAsync(Stream stream, CancellationToken token = default)
    {
        int comando = await LeerComandoAsync(stream, token);
        int largo = await LeerLargoAsync(stream, token);
        byte[] datos = await LeerExactamenteAsync(stream, largo, "DATOS", token);
        return new Trama(comando, datos);
    }

    public async Task EscribirAsync(Stream stream, Trama trama, CancellationToken token = default)
    {
        int largo = trama.Datos.Length;
        // La validación es previa a escribir: no se mandó nada todavía y la conexión puede seguir.
        if (largo > Limites.LargoMaximoDeDatos)
        {
            throw new PayloadDemasiadoGrandeException(largo, Limites.LargoMaximoDeDatos);
        }

        byte[] salida = new byte[LargoComando + LargoCampoLargo + largo];
        salida[0] = (byte)('0' + trama.Comando / 10);
        salida[1] = (byte)('0' + trama.Comando % 10);
        BinaryPrimitives.WriteInt32BigEndian(salida.AsSpan(LargoComando, LargoCampoLargo), largo);
        trama.Datos.CopyTo(salida.AsMemory(LargoComando + LargoCampoLargo));
        await stream.WriteAsync(salida, token);
    }

    private static async Task<int> LeerComandoAsync(Stream stream, CancellationToken token)
    {
        byte[] comando = await LeerInicioDeTramaAsync(stream, token);
        if (!EsDigito(comando[0]) || !EsDigito(comando[1]))
        {
            throw new ProtocoloException("CMD debe ser dos dígitos decimales");
        }

        return (comando[0] - '0') * 10 + comando[1] - '0';
    }

    // Cero bytes antes de empezar la trama: el otro lado cerró bien. A mitad de trama: quedó cortada.
    private static async Task<byte[]> LeerInicioDeTramaAsync(Stream stream, CancellationToken token)
    {
        byte[] buffer = new byte[LargoComando];
        int offset = 0;
        while (offset < buffer.Length)
        {
            int recibidos = await stream.ReadAsync(buffer.AsMemory(offset), token);
            if (recibidos == 0 && offset == 0)
            {
                throw new ConexionCerradaException();
            }

            if (recibidos == 0)
            {
                throw new TramaTruncadaException("CMD");
            }

            offset += recibidos;
        }

        return buffer;
    }

    private static async Task<int> LeerLargoAsync(Stream stream, CancellationToken token)
    {
        byte[] bytes = await LeerExactamenteAsync(stream, LargoCampoLargo, "LARGO", token);
        int largo = BinaryPrimitives.ReadInt32BigEndian(bytes);
        if (largo < 0 || largo > Limites.LargoMaximoDeDatos)
        {
            throw new ProtocoloException($"LARGO fuera de rango: {largo}");
        }

        return largo;
    }

    // ReadExactlyAsync insiste hasta completar el campo. Si la conexión se cierra antes, lanza
    // EndOfStreamException, que acá se traduce a trama truncada.
    private static async Task<byte[]> LeerExactamenteAsync(
        Stream stream, int cantidad, string campo, CancellationToken token)
    {
        byte[] buffer = new byte[cantidad];
        try
        {
            await stream.ReadExactlyAsync(buffer, token);
        }
        catch (EndOfStreamException)
        {
            throw new TramaTruncadaException(campo);
        }

        return buffer;
    }

    private static bool EsDigito(byte valor)
    {
        return valor is >= (byte)'0' and <= (byte)'9';
    }
}

// El DATOS de toda respuesta empieza con dos caracteres de estado y sigue con el contenido, que
// siempre es texto UTF-8. Un archivo nunca va en DATOS: viaja después de la trama, en partes.
public sealed class RespuestaDelServidor : IAsyncDisposable
{
    private readonly byte[] _contenido;
    // Stream.Null cuando la respuesta no lleva archivo: su largo es cero y no manda nada.
    private readonly Stream _archivo;

    private RespuestaDelServidor(string estado, byte[] contenido)
        : this(estado, contenido, Stream.Null)
    {
    }

    private RespuestaDelServidor(string estado, byte[] contenido, Stream archivo)
    {
        Estado = estado;
        _contenido = contenido;
        _archivo = archivo;
    }

    public string Estado { get; }

    public static RespuestaDelServidor Ok(string contenido)
    {
        return new RespuestaDelServidor(Estados.Ok, TextoUtf8.Codificar(contenido));
    }

    // El contenido solo anuncia el nombre y el largo; las partes salen después, con EnviarArchivoAsync.
    public static RespuestaDelServidor OkConArchivo(string nombreArchivo, Stream archivo)
    {
        ArchivoAnunciado anuncio = new(nombreArchivo, archivo.Length);
        byte[] contenido = TextoUtf8.Codificar(Mensajes.DeArchivoAnunciado(anuncio));
        return new RespuestaDelServidor(Estados.Ok, contenido, archivo);
    }

    public static RespuestaDelServidor Rechazo(string mensaje)
    {
        return new RespuestaDelServidor(Estados.Error, TextoUtf8.Codificar(mensaje));
    }

    public static RespuestaDelServidor Interno(string mensaje)
    {
        return new RespuestaDelServidor(Estados.Interno, TextoUtf8.Codificar(mensaje));
    }

    public static RespuestaDelServidor Decodificar(Trama respuesta)
    {
        if (respuesta.Datos.Length < Estados.Largo)
        {
            throw new ProtocoloException("La respuesta no contiene un ESTADO completo");
        }

        string estado = TextoUtf8.Decodificar(respuesta.Datos.Span[..Estados.Largo]);
        if (estado is not (Estados.Ok or Estados.Error or Estados.Interno))
        {
            throw new ProtocoloException($"ESTADO desconocido: '{estado}'");
        }

        return new RespuestaDelServidor(estado, respuesta.Datos.Span[Estados.Largo..].ToArray());
    }

    public byte[] Codificar()
    {
        byte[] estado = TextoUtf8.Codificar(Estado);
        byte[] datos = new byte[estado.Length + _contenido.Length];
        estado.CopyTo(datos, 0);
        _contenido.CopyTo(datos, estado.Length);
        return datos;
    }

    public string ContenidoComoTexto()
    {
        return TextoUtf8.Decodificar(_contenido);
    }

    public Task EnviarArchivoAsync(
        TransferenciaPorPartes transferencia, CancellationToken token = default)
    {
        return transferencia.EnviarAsync(_archivo, _archivo.Length, token);
    }

    public ValueTask DisposeAsync()
    {
        return _archivo.DisposeAsync();
    }
}
