using System.Net.Sockets;
using Cliente.Excepciones;
using Common.Excepciones;
using Common.Protocolo;

namespace Cliente;

internal sealed class ConexionConServidor : IDisposable
{
    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private readonly CodecDeTramas _codec;
    private readonly CanalDeArchivos _canalDeArchivos;
    private readonly long _maxFileBytes;
    private int _cerrada;

    public string DownloadsDirectory { get; }

    private ConexionConServidor(Socket socket, ConfiguracionCliente configuracion)
    {
        _socket = socket;
        // ownsSocket en false porque Dispose ya se encarga de cerrar el stream y el socket en orden.
        _stream = new NetworkStream(socket, ownsSocket: false);
        _codec = new CodecDeTramas(configuracion.MaxPayloadBytes);
        _canalDeArchivos = new CanalDeArchivos(configuracion.FileBlockBytes);
        _maxFileBytes = configuracion.MaxFileBytes;
        DownloadsDirectory = configuracion.DownloadsDirectory;
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

    public string EnviarSolicitud(int comando, byte[] datos)
    {
        Trama respuesta = EnviarYLeer(comando, datos);
        RespuestaDelServidor contenido = RespuestaDelServidor.Decodificar(respuesta.Datos.Span);
        return ReaccionesAEstado.Aplicar(contenido);
    }

    public byte[] EnviarSolicitudYLeerCuerpo(int comando, byte[] datos)
    {
        Trama respuesta = EnviarYLeer(comando, datos);
        RespuestaDelServidor contenido = RespuestaDelServidor.Decodificar(respuesta.Datos.Span);
        return ReaccionesAEstado.AplicarSobreCuerpo(contenido).ToArray();
    }

    public string EnviarSolicitudConArchivo(int comando, byte[] datos, string rutaOrigen)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _cerrada) != 0, this);
        // El archivo se revisa antes de anunciarlo. Anunciarlo y después no poder mandarlo rompe el protocolo.
        FileInfo archivo = ExigirArchivoEnviable(rutaOrigen);
        if (archivo.Length > _maxFileBytes)
            throw new OperacionRechazadaException(
                $"El archivo supera el tamaño máximo permitido ({_maxFileBytes} bytes)");

        _codec.Escribir(_stream, Trama.CrearSolicitud(comando, datos));
        using (FileStream origen = archivo.OpenRead())
            _canalDeArchivos.Enviar(_stream, origen, archivo.Length);

        Trama respuesta = LeerRespuesta(comando);
        RespuestaDelServidor contenido = RespuestaDelServidor.Decodificar(respuesta.Datos.Span);
        return ReaccionesAEstado.Aplicar(contenido);
    }

    public long RecibirArchivoEn(string rutaDestino)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _cerrada) != 0, this);
        string rutaTemporal = rutaDestino + ".tmp";
        try
        {
            long tamanio;
            using (FileStream destino = File.Create(rutaTemporal))
                tamanio = _canalDeArchivos.RecibirEn(_stream, destino, _maxFileBytes);

            File.Move(rutaTemporal, rutaDestino, overwrite: true);
            return tamanio;
        }
        catch
        {
            File.Delete(rutaTemporal);
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _cerrada, 1) != 0)
            return;

        IntentarShutdown();
        _stream.Dispose();
        _socket.Dispose();
    }

    private Trama EnviarYLeer(int comando, byte[] datos)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _cerrada) != 0, this);
        _codec.Escribir(_stream, Trama.CrearSolicitud(comando, datos));
        return LeerRespuesta(comando);
    }

    private Trama LeerRespuesta(int comando)
    {
        Trama respuesta = _codec.Leer(_stream);
        ExigirRespuestaA(comando, respuesta);
        return respuesta;
    }

    private static FileInfo ExigirArchivoEnviable(string rutaOrigen)
    {
        FileInfo archivo = new(rutaOrigen);
        if (!archivo.Exists)
            throw new FileNotFoundException("No se encontró el archivo a enviar", rutaOrigen);

        return archivo;
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
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
