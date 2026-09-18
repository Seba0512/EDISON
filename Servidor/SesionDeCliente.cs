using System.Net.Sockets;
using Common;
using Dominio;

namespace Servidor;

internal sealed class EstadoDeSesion
{
    private Usuario? _usuario;

    public bool TieneSesion => _usuario is not null;

    public Usuario UsuarioAutenticado
        => _usuario ?? throw new DominioException("La conexión no tiene una sesión iniciada");

    public void Autenticar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        if (TieneSesion)
        {
            throw new DominioException("La conexión ya tiene una sesión iniciada");
        }

        _usuario = usuario;
    }

    public void Cerrar()
    {
        _usuario = null;
    }
}

// Los archivos de CMD 15 y 16 viajan fuera de la trama, por el mismo stream de la sesión.
internal sealed class ArchivosDeSesion
{
    private readonly Stream _stream;
    private readonly CanalDeArchivos _canal;
    private readonly long _maxFileBytes;

    public ArchivosDeSesion(Stream stream, CanalDeArchivos canal, long maxFileBytes)
    {
        _stream = stream;
        _canal = canal;
        _maxFileBytes = maxFileBytes;
    }

    public long RecibirEn(Stream destino)
    {
        return _canal.RecibirEn(_stream, destino, _maxFileBytes);
    }

    public void EnviarDesde(Stream origen, long tamanio)
    {
        _canal.Enviar(_stream, origen, tamanio);
    }
}

internal sealed class SesionDeCliente
{
    private readonly ConexionActiva _conexion;
    private readonly CodecDeTramas _codec;
    private readonly EjecutorDeComandos _ejecutor;
    private readonly EstadoDeSesion _sesion = new();
    private readonly CanalDeArchivos _canalDeArchivos;
    private readonly long _maxFileBytes;

    public SesionDeCliente(
        ConexionActiva conexion,
        CodecDeTramas codec,
        EjecutorDeComandos ejecutor,
        CanalDeArchivos canalDeArchivos,
        long maxFileBytes)
    {
        _conexion = conexion;
        _codec = codec;
        _ejecutor = ejecutor;
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
            {
                return;
            }
        }
    }

    private bool AtenderSolicitud(NetworkStream stream, Trama solicitud, ArchivosDeSesion archivos)
    {
        RespuestaDelServidor? respuesta = null;
        try
        {
            try
            {
                respuesta = _ejecutor.Ejecutar(solicitud, _sesion, archivos);
                Informar($"CMD {solicitud.Comando:D2} -> {respuesta.Estado}");
                EscribirRespuesta(stream, solicitud.Comando, respuesta);
            }
            catch (PayloadDemasiadoGrandeException)
            {
                // La validación de tamaño es previa a escribir: no se mandó nada y la conexión sigue.
                Informar($"CMD {solicitud.Comando:D2} -> respuesta demasiado grande, se rechaza sin cerrar la conexión");
                EscribirRespuesta(stream, solicitud.Comando, RespuestaDelServidor.Rechazo(
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
                Informar($"provocó un error inesperado procesando CMD {solicitud.Comando:D2}: {exception}");
                IntentarResponderErrorInterno(stream, solicitud.Comando);
                return false;
            }

            // Fuera del try: si el envío posterior falla, se cierra sin intentar una segunda respuesta.
            respuesta.AccionPosterior?.Invoke(stream);
            return true;
        }
        finally
        {
            respuesta?.Dispose();
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
}
