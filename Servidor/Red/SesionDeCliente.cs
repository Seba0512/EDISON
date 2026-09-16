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
    private readonly CanalDeArchivos _canalDeArchivos;
    private readonly long _maxFileBytes;

    public SesionDeCliente(
        ConexionActiva conexion,
        CodecDeTramas codec,
        DespachadorDeComandos despachador,
        EstadoDeSesion sesion,
        CanalDeArchivos canalDeArchivos,
        long maxFileBytes)
    {
        _conexion = conexion;
        _codec = codec;
        _despachador = despachador;
        _sesion = sesion;
        _canalDeArchivos = canalDeArchivos;
        _maxFileBytes = maxFileBytes;
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
        ArchivosDeSesion archivos = new(stream, _canalDeArchivos, _maxFileBytes);
        while (_conexion.EstaAbierta)
        {
            Trama solicitud = _codec.Leer(stream);
            if (!AtenderSolicitud(stream, solicitud, archivos))
                return;
        }
    }

    private bool AtenderSolicitud(NetworkStream stream, Trama solicitud, ArchivosDeSesion archivos)
    {
        try
        {
            RespuestaDelServidor respuesta = _despachador.Despachar(solicitud, _sesion, archivos);
            Informar($"CMD {solicitud.Comando:D2} -> {respuesta.Estado}");
            EscribirRespuesta(stream, solicitud.Comando, respuesta);
            respuesta.AccionPosterior?.Invoke(stream);
            return true;
        }
        catch (TransferenciaDeArchivoException)
        {
            // La transferencia quedó a medias y ya no se sabe dónde empieza la próxima trama. Hay que cerrar.
            throw;
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
            // Avisar el error al cliente también puede fallar. La conexión se cierra igual.
        }
        catch (SocketException)
        {
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
            TransferenciaDeArchivoException => $"falló durante una transferencia de archivo: {exception.Message}",
            ProtocoloException => $"provocó un error de protocolo: {exception.Message}",
            ObjectDisposedException when !_conexion.EstaAbierta =>
                "finalizó la comunicación durante el cierre solicitado",
            IOException or SocketException => $"finalizó la comunicación: {exception.Message}",
            _ => $"provocó un error inesperado: {exception}"
        };
}
