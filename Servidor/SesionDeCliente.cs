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

    public async Task AtenderAsync(CancellationToken token)
    {
        try
        {
            bool conexionAbierta = true;
            while (conexionAbierta)
            {
                Trama solicitud = await _codec.LeerAsync(_stream, token);
                conexionAbierta = await AtenderSolicitudAsync(solicitud, token);
            }
        }
        catch (Exception exception)
        {
            Informar(Describir(exception));
        }
        finally
        {
            await _stream.DisposeAsync();
        }
    }

    // Paso intermedio de una subida: el OK le avisa al cliente que ya puede mandar las partes,
    // que se escriben en el destino a medida que llegan.
    public async Task RecibirArchivoAsync(
        int comando, Stream destino, long largo, CancellationToken token)
    {
        await using RespuestaDelServidor listo = RespuestaDelServidor.Ok("Listo para recibir el archivo");
        await EscribirRespuestaAsync(comando, listo, token);
        Informar($"CMD {comando:D2} -> recibiendo {largo} bytes por partes");
        await _archivos.RecibirAsync(destino, largo, token);
    }

    private async Task<bool> AtenderSolicitudAsync(Trama solicitud, CancellationToken token)
    {
        try
        {
            await using RespuestaDelServidor respuesta =
                await _ejecutor.EjecutarAsync(solicitud, this, token);
            Informar($"CMD {solicitud.Comando:D2} -> {respuesta.Estado}");
            await EscribirRespuestaAsync(solicitud.Comando, respuesta, token);
            return true;
        }
        catch (PayloadDemasiadoGrandeException)
        {
            // La validación de tamaño es previa a escribir: no se mandó nada y la conexión sigue.
            Informar($"CMD {solicitud.Comando:D2} -> respuesta demasiado grande, se rechaza sin cerrar la conexión");
            await using RespuestaDelServidor rechazo = RespuestaDelServidor.Rechazo(
                "El resultado supera el tamaño máximo de respuesta; refine la búsqueda");
            await EscribirRespuestaAsync(solicitud.Comando, rechazo, token);
            return true;
        }
        catch (Exception exception) when (
            exception is not (IOException or SocketException or OperationCanceledException))
        {
            Informar($"provocó un error inesperado procesando CMD {solicitud.Comando:D2}: {exception}");
            await IntentarResponderErrorInternoAsync(solicitud.Comando, token);
            return false;
        }
    }

    // Si la respuesta lleva un archivo, sus partes salen inmediatamente después de la trama. Una
    // falla del disco a mitad del envío ya no se puede avisar: la conexión se corta.
    private async Task EscribirRespuestaAsync(
        int comando, RespuestaDelServidor respuesta, CancellationToken token)
    {
        await _codec.EscribirAsync(_stream, new Trama(comando, respuesta.Codificar()), token);
        await respuesta.EnviarArchivoAsync(_archivos, token);
    }

    private async Task IntentarResponderErrorInternoAsync(int comando, CancellationToken token)
    {
        try
        {
            await using RespuestaDelServidor interno =
                RespuestaDelServidor.Interno("Error interno del servidor");
            await EscribirRespuestaAsync(comando, interno, token);
        }
        catch (IOException)
        {
            // Avisar el error al cliente también puede fallar. La conexión se cierra igual.
        }
        catch (SocketException)
        {
        }
    }

    // Cada línea dice qué conexión la produjo, así con dos clientes a la vez se ve que el servidor
    // los atiende en paralelo. No se muestra un id de Task: en un método async Task.CurrentId es nulo.
    private void Informar(string mensaje)
    {
        Console.WriteLine($"[{Identidad()}] {mensaje}");
    }

    // El número identifica la conexión (cambia en cada reconexión) y el usuario, quién la está usando.
    private string Identidad()
    {
        string conexion = $"Cliente {_numero}";
        return UsuarioAutenticado is null ? conexion : $"{conexion} | {UsuarioAutenticado.NombreUsuario}";
    }

    private static string Describir(Exception exception)
    {
        return exception switch
        {
            ConexionCerradaException => "cerró la conexión",
            TramaTruncadaException => $"envió una trama truncada: {exception.Message}",
            ProtocoloException => $"provocó un error de protocolo: {exception.Message}",
            ObjectDisposedException or OperationCanceledException =>
                "finalizó la comunicación durante el cierre solicitado",
            IOException or SocketException => "finalizó la comunicación",
            _ => $"provocó un error inesperado: {exception}"
        };
    }
}
