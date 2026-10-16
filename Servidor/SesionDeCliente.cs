using System.Net.Sockets;
using Common;
using Servidor.Dominio.Entidades;

namespace Servidor;

internal sealed class SesionDeCliente
{
    private readonly Socket _socket;
    private readonly int _numero;
    private readonly CodecDeTramas _codec;
    private readonly EjecutorDeComandos _ejecutor;

    public SesionDeCliente(Socket socket, int numero, CodecDeTramas codec, EjecutorDeComandos ejecutor)
    {
        _socket = socket;
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
            using NetworkStream stream = new(_socket, ownsSocket: false);
            bool conexionAbierta = true;
            while (conexionAbierta)
            {
                Trama solicitud = _codec.Leer(stream);
                conexionAbierta = AtenderSolicitud(stream, solicitud);
            }
        }
        catch (Exception exception)
        {
            Informar(Describir(exception));
        }
    }

    private bool AtenderSolicitud(NetworkStream stream, Trama solicitud)
    {
        try
        {
            RespuestaDelServidor respuesta = _ejecutor.Ejecutar(solicitud, this);
            Informar($"CMD {solicitud.Comando:D2} -> {respuesta.Estado}");
            EscribirRespuesta(stream, solicitud.Comando, respuesta);
            return true;
        }
        catch (PayloadDemasiadoGrandeException)
        {
            // La validación de tamaño es previa a escribir: no se mandó nada y la conexión sigue.
            Informar($"CMD {solicitud.Comando:D2} -> respuesta demasiado grande, se rechaza sin cerrar la conexión");
            EscribirRespuesta(stream, solicitud.Comando, RespuestaDelServidor.Rechazo(
                "El resultado supera el tamaño máximo de respuesta; refine la búsqueda"));
            return true;
        }
        catch (Exception exception) when (exception is not IOException and not SocketException)
        {
            Informar($"provocó un error inesperado procesando CMD {solicitud.Comando:D2}: {exception}");
            IntentarResponderErrorInterno(stream, solicitud.Comando);
            return false;
        }
    }

    private void EscribirRespuesta(NetworkStream stream, int comando, RespuestaDelServidor respuesta)
    {
        _codec.Escribir(stream, new Trama(comando, respuesta.Codificar()));
    }

    private void IntentarResponderErrorInterno(NetworkStream stream, int comando)
    {
        try
        {
            EscribirRespuesta(stream, comando, RespuestaDelServidor.Interno("Error interno del servidor"));
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
