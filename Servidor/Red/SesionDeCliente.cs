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
    private readonly EstadoDeSesion _estado = new EstadoDeSesion();

    public SesionDeCliente(
        ConexionActiva conexion,
        CodecDeTramas codec,
        DespachadorDeComandos despachador)
    {
        _conexion = conexion;
        _codec = codec;
        _despachador = despachador;
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
            if (!ProcesarSolicitud(stream, solicitud))
                return;
        }
    }

    private bool ProcesarSolicitud(NetworkStream stream, Trama solicitud)
    {
        RespuestaDeOperacion resultado;
        Trama respuesta;
        try
        {
            resultado = _despachador.Despachar(solicitud, _estado);
            respuesta = Trama.CrearRespuesta(solicitud.Comando, resultado.Codificar());
        }
        catch (Exception exception)
        {
            Informar($"provocó un error inesperado: {exception}");
            IntentarResponderErrorInterno(stream, solicitud.Comando);
            return false;
        }

        _codec.Escribir(stream, respuesta);
        Informar($"procesó CMD {solicitud.Comando:D2} con estado {resultado.Estado}");
        return true;
    }

    private void IntentarResponderErrorInterno(NetworkStream stream, int comando)
    {
        try
        {
            byte[] datos = RespuestaDeOperacion.ErrorInterno().Codificar();
            _codec.Escribir(stream, Trama.CrearRespuesta(comando, datos));
        }
        catch (Exception exception)
        {
            Informar($"no pudo enviar la respuesta IN: {exception.Message}");
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
