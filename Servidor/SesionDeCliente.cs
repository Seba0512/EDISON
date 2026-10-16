using System.Net.Sockets;
using Common;
using Servidor.Dominio.Entidades;

namespace Servidor;

internal sealed class SesionDeCliente
{
    private readonly NetworkStream _stream;
    private readonly TransferenciaPorPartes _archivos;
    private readonly int _numero;
    private readonly CodecDeTramas _codec;
    private readonly EjecutorDeComandos _ejecutor;

    // Se crea con el socket recién aceptado (ver ServidorTcp.IncorporarCliente).
    public SesionDeCliente(Socket socket, int numero, CodecDeTramas codec, EjecutorDeComandos ejecutor)
    {
        _stream = new NetworkStream(socket, ownsSocket: false);
        _archivos = new TransferenciaPorPartes(_stream);
        _numero = numero;
        _codec = codec;
        _ejecutor = ejecutor;
    }

    // La sesión de la conexión, como la plantea el curso: null hasta un login exitoso.
    public Usuario? UsuarioAutenticado { get; set; }

    public void Atender()
    {
        try
        {
            bool conexionAbierta = true;
            while (conexionAbierta)
            {
                Trama solicitud = _codec.Leer(_stream);
                conexionAbierta = AtenderSolicitud(solicitud);
            }
        }
        catch (Exception exception)
        {
            Informar(Describir(exception));
        }
        finally
        {
            _stream.Dispose();
        }
    }

    // Paso intermedio de una subida: el OK le avisa al cliente que ya puede mandar las partes,
    // que se escriben en el destino a medida que llegan.
    public void RecibirArchivo(int comando, Stream destino, long largo)
    {
        EscribirRespuesta(comando, RespuestaDelServidor.Ok("Listo para recibir el archivo"));
        Informar($"CMD {comando:D2} -> recibiendo {largo} bytes por partes");
        _archivos.Recibir(destino, largo);
    }

    private bool AtenderSolicitud(Trama solicitud)
    {
        try
        {
            using RespuestaDelServidor respuesta = _ejecutor.Ejecutar(solicitud, this);
            Informar($"CMD {solicitud.Comando:D2} -> {respuesta.Estado}");
            EscribirRespuesta(solicitud.Comando, respuesta);
            return true;
        }
        catch (PayloadDemasiadoGrandeException)
        {
            // La validación de tamaño es previa a escribir: no se mandó nada y la conexión sigue.
            Informar($"CMD {solicitud.Comando:D2} -> respuesta demasiado grande, se rechaza sin cerrar la conexión");
            EscribirRespuesta(solicitud.Comando, RespuestaDelServidor.Rechazo(
                "El resultado supera el tamaño máximo de respuesta; refine la búsqueda"));
            return true;
        }
        catch (Exception exception) when (exception is not IOException and not SocketException)
        {
            Informar($"provocó un error inesperado procesando CMD {solicitud.Comando:D2}: {exception}");
            IntentarResponderErrorInterno(solicitud.Comando);
            return false;
        }
    }

    // Si la respuesta lleva un archivo, sus partes salen inmediatamente después de la trama. Una
    // falla del disco a mitad del envío ya no se puede avisar: la conexión se corta.
    private void EscribirRespuesta(int comando, RespuestaDelServidor respuesta)
    {
        _codec.Escribir(_stream, new Trama(comando, respuesta.Codificar()));
        respuesta.EnviarArchivo(_archivos);
    }

    private void IntentarResponderErrorInterno(int comando)
    {
        try
        {
            EscribirRespuesta(comando, RespuestaDelServidor.Interno("Error interno del servidor"));
        }
        catch (IOException)
        {
            // Avisar el error al cliente también puede fallar. La conexión se cierra igual.
        }
        catch (SocketException)
        {
        }
    }

    private void Informar(string mensaje)
    {
        Console.WriteLine($"[{Identidad()} | Thread {Environment.CurrentManagedThreadId}] {mensaje}");
    }

    // Con sesión iniciada identifica por nombre de usuario. El número cambia en cada reconexión.
    private string Identidad()
    {
        return UsuarioAutenticado?.NombreUsuario ?? $"Cliente {_numero}";
    }

    private static string Describir(Exception exception)
    {
        return exception switch
        {
            ConexionCerradaException => "cerró la conexión",
            TramaTruncadaException => $"envió una trama truncada: {exception.Message}",
            ProtocoloException => $"provocó un error de protocolo: {exception.Message}",
            ObjectDisposedException => "finalizó la comunicación durante el cierre solicitado",
            IOException or SocketException => "finalizó la comunicación",
            _ => $"provocó un error inesperado: {exception}"
        };
    }
}
