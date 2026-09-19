using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Common;

namespace Cliente;

internal sealed class ConexionConServidor : IDisposable
{
    private const string SufijoTemporal = ".tmp";

    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private readonly CodecDeTramas _codec;
    private readonly CanalDeArchivos _canalDeArchivos;
    private readonly long _maxFileBytes;
    private readonly string _downloadsDirectory;
    private readonly object _candadoDeSocket = new();
    private int _cerrada;

    private ConexionConServidor(Socket socket, ConfiguracionCliente configuracion)
    {
        _socket = socket;
        // ownsSocket en false porque Dispose ya se encarga de cerrar el stream y el socket en orden.
        _stream = new NetworkStream(socket, ownsSocket: false);
        _codec = new CodecDeTramas(configuracion.MaxPayloadBytes);
        _canalDeArchivos = new CanalDeArchivos(configuracion.FileBlockBytes);
        _maxFileBytes = configuracion.MaxFileBytes;
        _downloadsDirectory = Path.GetFullPath(configuracion.DownloadsDirectory);
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

    public string Registrar(Credenciales credenciales)
        => Enviar(Comandos.Registrar, Mensajes.DeCredenciales(credenciales));

    public string IniciarSesion(Credenciales credenciales)
        => Enviar(Comandos.IniciarSesion, Mensajes.DeCredenciales(credenciales));

    public string CerrarSesion()
        => Enviar(Comandos.CerrarSesion, string.Empty);

    public string AltaDeModelo(DatosDeAltaDeModelo datos)
        => Enviar(Comandos.AltaDeModelo, Mensajes.DeDatosDeModelo(datos));

    public string ModificarModelo(DatosDeAltaDeModelo datos)
        => Enviar(Comandos.ModificarModelo, Mensajes.DeDatosDeModelo(datos));

    public string EliminarModelo(string nombreModelo)
        => Enviar(Comandos.BajaDeModelo, nombreModelo);

    public IReadOnlyList<ModeloListado> Buscar(FiltrosDeBusqueda filtros)
        => Mensajes.AModelosListados(Enviar(Comandos.BuscarModelos, Mensajes.DeFiltros(filtros)));

    public DetalleDeModelo Consultar(string nombreModelo)
        => Mensajes.ADetalleDeModelo(Enviar(Comandos.ConsultarModelo, nombreModelo));

    public string SolicitarReserva(string nombreModelo)
        => Enviar(Comandos.SolicitarReserva, nombreModelo);

    public string CancelarReserva(Guid idReserva)
        => Enviar(Comandos.CancelarReserva, FormatoDeCampos.De(idReserva));

    public IReadOnlyList<ReservaListada> MisReservas()
        => Mensajes.AReservasListadas(Enviar(Comandos.ConsultarMisReservas, string.Empty));

    public IReadOnlyList<ActividadListada> Historial()
        => Mensajes.AActividades(Enviar(Comandos.ConsultarHistorial, string.Empty));

    public string SubirImagen(string nombreModelo, string rutaArchivo)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _cerrada) != 0, this);
        // El tamaño se anuncia desde el stream ya abierto. Medirlo antes de abrir permitiría
        // anunciar un tamaño viejo si el archivo cambia entre medio y romper el protocolo.
        using FileStream origen = AbrirArchivoLocal(rutaArchivo);
        if (origen.Length > _maxFileBytes)
        {
            throw new OperacionRechazadaException(
                $"El archivo supera el tamaño máximo permitido ({_maxFileBytes} bytes)");
        }

        string mensaje = Mensajes.DeSubidaDeImagen(nombreModelo, Path.GetFileName(rutaArchivo));
        Trama solicitud = new(Comandos.SubirImagen, TextoUtf8.Codificar(mensaje));
        lock (_candadoDeSocket)
        {
            _codec.Escribir(_stream, solicitud);
            _canalDeArchivos.Enviar(_stream, origen, origen.Length);
            return Interpretar(LeerRespuesta(Comandos.SubirImagen));
        }
    }

    // Un problema local con el archivo todavía no tocó la red, así que la sesión puede continuar.
    private static FileStream AbrirArchivoLocal(string rutaArchivo)
    {
        try
        {
            return new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            throw new OperacionRechazadaException($"No se pudo leer el archivo: {excepcion.Message}");
        }
    }

    public string DescargarImagen(string nombreModelo)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _cerrada) != 0, this);
        lock (_candadoDeSocket)
        {
            Trama solicitud = new(Comandos.DescargarImagen, TextoUtf8.Codificar(nombreModelo));
            _codec.Escribir(_stream, solicitud);
            string nombreSugerido = Interpretar(LeerRespuesta(Comandos.DescargarImagen));
            return RecibirImagen(nombreSugerido);
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

    // Desde el OK de CMD 16 hay bytes binarios pendientes en el stream. Ante cualquier falla se
    // invalida la conexión para no interpretar esos bytes como tramas y se borra el temporal.
    private string RecibirImagen(string nombreSugerido)
    {
        string? rutaTemporal = null;
        try
        {
            string nombreArchivo = NombreDeDescargaSeguro.Crear(nombreSugerido);
            Directory.CreateDirectory(_downloadsDirectory);
            string rutaDestino = Path.Combine(_downloadsDirectory, nombreArchivo);
            rutaTemporal = rutaDestino + SufijoTemporal;

            long tamanio;
            using (FileStream destino = File.Create(rutaTemporal))
                tamanio = _canalDeArchivos.RecibirEn(_stream, destino, _maxFileBytes);

            File.Move(rutaTemporal, rutaDestino, overwrite: true);
            return $"Imagen guardada en '{rutaDestino}' ({tamanio} bytes)";
        }
        catch
        {
            IntentarShutdown();
            IntentarEliminarTemporal(rutaTemporal);
            throw;
        }
    }

    private string Enviar(int comando, string mensaje)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _cerrada) != 0, this);
        lock (_candadoDeSocket)
        {
            _codec.Escribir(_stream, new Trama(comando, TextoUtf8.Codificar(mensaje)));
            return Interpretar(LeerRespuesta(comando));
        }
    }

    private Trama LeerRespuesta(int comando)
    {
        Trama respuesta = _codec.Leer(_stream);
        // Sin header REQ/RES la única verificación es que la respuesta traiga el comando enviado.
        if (respuesta.Comando != comando)
        {
            throw new ProtocoloException(
                $"El cliente recibió el CMD inesperado {respuesta.Comando:D2}");
        }

        return respuesta;
    }

    private static string Interpretar(Trama respuesta)
    {
        RespuestaDelServidor contenido = RespuestaDelServidor.Decodificar(respuesta);
        if (contenido.Estado == Estados.Error)
            throw new OperacionRechazadaException(contenido.Contenido);
        if (contenido.Estado == Estados.Interno)
            throw new ErrorInternoDelServidorException(contenido.Contenido);

        return contenido.Contenido;
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

    private static void IntentarEliminarTemporal(string? rutaTemporal)
    {
        if (rutaTemporal is null || !File.Exists(rutaTemporal))
            return;

        try
        {
            File.Delete(rutaTemporal);
        }
        catch (Exception excepcion)
        {
            Console.Error.WriteLine(
                $"No se pudo limpiar el archivo temporal '{rutaTemporal}': {excepcion.Message}");
        }
    }
}

// El nombre de la imagen lo anuncia el servidor: acá se convierte en un nombre de archivo que
// no pueda escaparse del directorio de descargas ni chocar con las reglas de Windows.
internal static class NombreDeDescargaSeguro
{
    private const int LargoMaximoComponenteWindows = 255;
    private const string SufijoTemporal = ".tmp";
    private const string PrefijoNombreConHash = "modelo-";

    private static readonly HashSet<char> CaracteresInvalidos =
        new("<>:\"/\\|?*".Concat(Enumerable.Range(0, 32).Select(valor => (char)valor)));

    private static readonly HashSet<string> NombresReservados = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³"
    };

    private static readonly HashSet<string> ExtensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };

    public static string Crear(string nombreSugerido)
    {
        (string nombreLogico, string extension) = Separar(nombreSugerido);
        int largoMaximoNombre = LargoMaximoComponenteWindows - extension.Length - SufijoTemporal.Length;
        string nombreFisico = CodificarConEscape(nombreLogico);
        if (nombreFisico.Length > largoMaximoNombre)
            nombreFisico = CrearNombreConHash(nombreLogico);

        if (nombreFisico.Length > largoMaximoNombre)
            throw new ProtocoloException("El nombre de imagen anunciado es demasiado largo");

        return nombreFisico + extension;
    }

    private static (string NombreLogico, string Extension) Separar(string nombreSugerido)
    {
        int inicioExtension = nombreSugerido.LastIndexOf('.');
        if (inicioExtension <= 0)
            throw new ProtocoloException("La imagen anunciada no tiene una extensión válida");

        string nombreLogico = nombreSugerido[..inicioExtension];
        string extension = nombreSugerido[inicioExtension..];
        if (!ExtensionesPermitidas.Contains(extension))
        {
            throw new ProtocoloException(
                $"La extensión de imagen anunciada '{extension}' no está permitida");
        }

        return (nombreLogico, extension);
    }

    private static string CodificarConEscape(string nombreLogico)
    {
        bool reservado = EsReservado(nombreLogico);
        StringBuilder resultado = new(nombreLogico.Length);
        for (int indice = 0; indice < nombreLogico.Length; indice++)
        {
            char caracter = nombreLogico[indice];
            bool esUltimo = indice == nombreLogico.Length - 1;
            bool debeEscaparse = caracter == '%' || CaracteresInvalidos.Contains(caracter)
                                 || esUltimo && caracter is '.' or ' '
                                 || indice == 0 && reservado;

            resultado.Append(debeEscaparse ? $"%{(int)caracter:X4}" : caracter);
        }

        return resultado.ToString();
    }

    private static string CrearNombreConHash(string nombreLogico)
    {
        byte[] hash = SHA256.HashData(TextoUtf8.Codificar(nombreLogico));
        return PrefijoNombreConHash + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool EsReservado(string nombre)
        => NombresReservados.Contains(nombre.Split('.')[0].TrimEnd(' '));
}
