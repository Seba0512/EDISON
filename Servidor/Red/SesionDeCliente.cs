using System.Net.Sockets;
using Common.Excepciones;
using Common.Protocolo;

namespace Servidor.Red;

internal sealed class SesionDeCliente
{
    private readonly ConexionActiva _conexion;
    private readonly CodecDeTramas _codec;

    public SesionDeCliente(ConexionActiva conexion, CodecDeTramas codec)
    {
        _conexion = conexion;
        _codec = codec;
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
            ExigirDiagnostico(solicitud);
            string texto = TextoUtf8.Decodificar(solicitud.Datos.Span);
            Informar($"envió diagnóstico '{texto}'");
            Trama respuesta = Trama.CrearRespuesta(
                ComandosTecnicos.Diagnostico, TextoUtf8.Codificar(texto));
            _codec.Escribir(stream, respuesta);
        }
    }

    private static void ExigirDiagnostico(Trama solicitud)
    {
        if (!solicitud.EsSolicitud)
            throw new ProtocoloException("El servidor esperaba HEADER REQ");
        if (solicitud.Comando != ComandosTecnicos.Diagnostico)
            throw new ProtocoloException(
                $"CMD {solicitud.Comando:D2} no está soportado en esta etapa");
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
