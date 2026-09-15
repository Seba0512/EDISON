using System.Net.Sockets;
using Common.Excepciones;
using Common.Protocolo;

namespace Cliente;

internal sealed class ConexionConServidor : IDisposable
{
    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private readonly CodecDeTramas _codec;
    private int _cerrada;

    private ConexionConServidor(Socket socket, ConfiguracionCliente configuracion)
    {
        _socket = socket;
        _stream = new NetworkStream(socket, ownsSocket: false);
        _codec = new CodecDeTramas(configuracion.MaxPayloadBytes);
    }

    public static ConexionConServidor Conectar(ConfiguracionCliente configuracion)
    {
        Socket socket = new(
            configuracion.ServerIp.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);
        try
        {
            socket.Bind(configuracion.LocalEndpoint);
            socket.Connect(configuracion.Endpoint);
            return new ConexionConServidor(socket, configuracion);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public string EnviarDiagnostico(string texto)
        => EnviarSolicitud(Comandos.Diagnostico, TextoUtf8.Codificar(texto));

    public string Registrar(Credenciales credenciales)
        => EnviarSolicitud(Comandos.Registrar, CodecDeCredenciales.Codificar(credenciales));

    public string IniciarSesion(Credenciales credenciales)
        => EnviarSolicitud(Comandos.IniciarSesion, CodecDeCredenciales.Codificar(credenciales));

    public string CerrarSesion()
        => EnviarSolicitud(Comandos.CerrarSesion, []);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _cerrada, 1) != 0)
            return;

        IntentarShutdown();
        _stream.Dispose();
        _socket.Dispose();
    }

    private string EnviarSolicitud(int comando, byte[] datos)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _cerrada) != 0, this);
        _codec.Escribir(_stream, Trama.CrearSolicitud(comando, datos));
        Trama respuesta = _codec.Leer(_stream);
        ExigirRespuestaA(comando, respuesta);
        RespuestaDelServidor contenido = RespuestaDelServidor.Decodificar(respuesta.Datos.Span);
        return ReaccionesAEstado.Aplicar(contenido);
    }

    private static void ExigirRespuestaA(int comando, Trama respuesta)
    {
        if (!respuesta.EsRespuesta)
            throw new ProtocoloException("El cliente esperaba HEADER RES");
        if (respuesta.Comando != comando)
            throw new ProtocoloException(
                $"El cliente recibió el CMD inesperado {respuesta.Comando:D2}");
    }

    private void IntentarShutdown()
    {
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {
            // El extremo remoto puede haber cerrado primero.
        }
        catch (ObjectDisposedException)
        {
            // Dispose es idempotente.
        }
    }
}
