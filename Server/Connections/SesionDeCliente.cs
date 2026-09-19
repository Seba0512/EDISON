using System.Net.Sockets;
using Common;
using Server.Commands;

namespace Server.Connections;

// Cuerpo del Thread de un cliente. Cualquier error queda contenido en esta conexión: se registra,
// se cierra el socket y el resto del servidor sigue atendiendo.
internal sealed class SesionDeCliente
{
    private readonly ConexionActiva _conexion;
    private readonly RecursosDeSesion _recursos;
    private readonly EstadoDeSesion _sesion = new();

    public SesionDeCliente(ConexionActiva conexion, RecursosDeSesion recursos)
    {
        _conexion = conexion;
        _recursos = recursos;
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
        ArchivosDeSesion archivos = new(stream, _recursos.CanalDeArchivos, _recursos.MaxFileBytes);
        ContextoDeConexion contexto = new(_sesion, archivos);
        while (_conexion.EstaAbierta)
        {
            Trama solicitud = _recursos.Codec.Leer(stream);
            if (!AtenderSolicitud(new SolicitudEnCurso(stream, solicitud, contexto)))
            {
                return;
            }
        }
    }

    private bool AtenderSolicitud(SolicitudEnCurso enCurso)
    {
        RespuestaDelServidor? respuesta = null;
        try
        {
            try
            {
                respuesta = _recursos.Despachador.Ejecutar(enCurso.Solicitud, enCurso.Contexto);
                Informar($"CMD {enCurso.Comando:D2} -> {respuesta.Estado}");
                EscribirRespuesta(enCurso, respuesta);
            }
            catch (PayloadDemasiadoGrandeException)
            {
                // La validación de tamaño es previa a escribir: no se mandó nada y la conexión sigue.
                Informar($"CMD {enCurso.Comando:D2} -> respuesta demasiado grande, se rechaza sin cerrar la conexión");
                EscribirRespuesta(enCurso, RespuestaDelServidor.Rechazo(
                    "El resultado supera el tamaño máximo de respuesta; refine la búsqueda"));
                return true;
            }
            catch (TransferenciaDeArchivoException)
            {
                // La transferencia quedó a medias y ya no se sabe dónde empieza la próxima trama.
                throw;
            }
            catch (Exception exception) when (exception is not IOException and not SocketException)
            {
                Informar($"provocó un error inesperado procesando CMD {enCurso.Comando:D2}: {exception}");
                IntentarResponderErrorInterno(enCurso);
                return false;
            }

            // Fuera del try: si el envío posterior falla, se cierra sin intentar una segunda respuesta.
            respuesta.AccionPosterior?.Invoke(enCurso.Stream);
            return true;
        }
        finally
        {
            respuesta?.Dispose();
        }
    }

    private void EscribirRespuesta(SolicitudEnCurso enCurso, RespuestaDelServidor respuesta)
    {
        _recursos.Codec.Escribir(enCurso.Stream, new Trama(enCurso.Comando, respuesta.Codificar()));
    }

    private void IntentarResponderErrorInterno(SolicitudEnCurso enCurso)
    {
        try
        {
            EscribirRespuesta(enCurso, RespuestaDelServidor.Interno("Error interno del servidor"));
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
        return _sesion.TieneSesion
            ? _sesion.UsuarioAutenticado.NombreUsuario
            : $"Cliente {_conexion.Numero}";
    }

    private string Describir(Exception exception)
    {
        return exception switch
        {
            ConexionCerradaException => "cerró la conexión",
            TramaTruncadaException => $"envió una trama truncada: {exception.Message}",
            TransferenciaDeArchivoException => $"falló durante una transferencia de archivo: {exception.Message}",
            ProtocoloException => $"provocó un error de protocolo: {exception.Message}",
            ObjectDisposedException when !_conexion.EstaAbierta =>
                "finalizó la comunicación durante el cierre solicitado",
            IOException or SocketException => "finalizó la comunicación",
            _ => $"provocó un error inesperado: {exception}"
        };
    }

    private sealed record SolicitudEnCurso(
        NetworkStream Stream, Trama Solicitud, ContextoDeConexion Contexto)
    {
        public int Comando => Solicitud.Comando;
    }
}
