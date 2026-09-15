using System.Net.Sockets;
using Common.Excepciones;
using Common.Protocolo;
using Servidor.Comandos;

namespace Servidor.Red;

internal sealed class SesionDeCliente
{
    private readonly ConexionActiva _conexion;
    private readonly CodecDeTramas _codec;
    private readonly DespachadorDeComandos _despachador;
    private readonly EstadoDeSesion _sesion;

    public SesionDeCliente(
        ConexionActiva conexion,
        CodecDeTramas codec,
        DespachadorDeComandos despachador,
        EstadoDeSesion sesion)
    {
        _conexion = conexion;
        _codec = codec;
        _despachador = despachador;
        _sesion = sesion;
    }

    public void Atender()
    {
        try
        {
            using NetworkStream stream = new(_conexion.Socket, ownsSocket: false);
            AtenderMensajes(stream);
        }
        catch (Exception exception)
        {
            Informar(Describir(exception));
        }
        finally
        {
            _conexion.Cerrar();
        }
    }

    private void AtenderMensajes(NetworkStream stream)
    {
        while (_conexion.EstaAbierta)
        {
            Trama solicitud = _codec.Leer(stream);
            if (!AtenderSolicitud(stream, solicitud))
                return;
        }
    }

    private bool AtenderSolicitud(NetworkStream stream, Trama solicitud)
    {
        try
        {
            RespuestaDelServidor respuesta = _despachador.Despachar(solicitud, _sesion);
            Informar($"CMD {solicitud.Comando:D2} -> {respuesta.Estado}");
            EscribirRespuesta(stream, solicitud.Comando, respuesta);
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
        => _codec.Escribir(stream, Trama.CrearRespuesta(comando, respuesta.Codificar()));

    private void IntentarResponderErrorInterno(NetworkStream stream, int comando)
    {
        try
        {
            EscribirRespuesta(stream, comando, RespuestaDelServidor.Interno("Error interno del servidor"));
        }
        catch (IOException)
        {
            // El intento de avisar al cliente también puede fallar; la conexión se cierra igual.
        }
        catch (SocketException)
        {
            // Idem.
        }
    }

    private void Informar(string mensaje)
        => Console.WriteLine(
            $"[Cliente {_conexion.Numero} | Thread {Environment.CurrentManagedThreadId}] {mensaje}");

    private string Describir(Exception exception)
        => exception switch
        {
            ConexionCerradaException => "cerró la conexión",
            TramaTruncadaException => $"envió una trama truncada: {exception.Message}",
            ProtocoloException => $"provocó un error de protocolo: {exception.Message}",
            ObjectDisposedException when !_conexion.EstaAbierta =>
                "finalizó la comunicación durante el cierre solicitado",
            IOException or SocketException => $"finalizó la comunicación: {exception.Message}",
            _ => $"provocó un error inesperado: {exception}"
        };
}
