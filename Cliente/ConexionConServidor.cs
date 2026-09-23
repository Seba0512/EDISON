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
    private readonly long _maxFileBytes;
    private readonly string _downloadsDirectory;
    private bool _cerrada;

    private ConexionConServidor(Socket socket, ConfiguracionCliente configuracion)
    {
        _socket = socket;
        // ownsSocket en false porque Dispose ya se encarga de cerrar el stream y el socket en orden.
        _stream = new NetworkStream(socket, ownsSocket: false);
        _codec = new CodecDeTramas(configuracion.MaxPayloadBytes);
        _maxFileBytes = configuracion.MaxFileBytes;
        _downloadsDirectory = Path.GetFullPath(configuracion.DownloadsDirectory);
    }

    public static ConexionConServidor Conectar(ConfiguracionCliente configuracion)
    {
        // Sin Bind local: el sistema operativo asigna el puerto efímero, así pueden abrirse varias
        // instancias del cliente y conectarse a un servidor en otra máquina sin tocar código.
        Socket socket = new(
            configuracion.ServerIp.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);
        try
        {
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

    // La imagen viaja dentro de DATOS: primero el texto "modelo#archivo" y después sus bytes.
    public string SubirImagen(string nombreModelo, string rutaArchivo)
    {
        byte[] imagen = LeerArchivoLocal(rutaArchivo);
        string mensaje = Mensajes.DeSubidaDeImagen(nombreModelo, Path.GetFileName(rutaArchivo));
        return Solicitar(Comandos.SubirImagen, Mensajes.DeTextoYBinario(mensaje, imagen))
            .ContenidoComoTexto();
    }

    public string DescargarImagen(string nombreModelo)
    {
        RespuestaDelServidor respuesta =
            Solicitar(Comandos.DescargarImagen, TextoUtf8.Codificar(nombreModelo));
        (string nombreSugerido, byte[] imagen) = Mensajes.ATextoYBinario(respuesta.ContenidoCrudo.Span);
        return GuardarImagen(nombreSugerido, imagen);
    }

    public void Dispose()
    {
        if (_cerrada)
            return;

        _cerrada = true;
        IntentarShutdown();
        _stream.Dispose();
        _socket.Dispose();
    }

    // Un problema local con el archivo todavía no tocó la red, así que la sesión puede continuar.
    private byte[] LeerArchivoLocal(string rutaArchivo)
    {
        try
        {
            byte[] imagen = File.ReadAllBytes(rutaArchivo);
            if (imagen.LongLength > _maxFileBytes)
            {
                throw new OperacionRechazadaException(
                    $"El archivo supera el tamaño máximo permitido ({_maxFileBytes} bytes)");
            }

            return imagen;
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            throw new OperacionRechazadaException($"No se pudo leer el archivo: {excepcion.Message}");
        }
    }

    // La imagen ya llegó entera en la trama: si falla el guardado local, la conexión queda sana.
    private string GuardarImagen(string nombreSugerido, byte[] imagen)
    {
        string? rutaTemporal = null;
        try
        {
            string nombreArchivo = NombreDeDescargaSeguro.Crear(nombreSugerido);
            Directory.CreateDirectory(_downloadsDirectory);
            string rutaDestino = Path.Combine(_downloadsDirectory, nombreArchivo);
            rutaTemporal = rutaDestino + SufijoTemporal;
            // Se escribe a un .tmp y luego se mueve, para no dejar una imagen a medias si falla la copia.
            File.WriteAllBytes(rutaTemporal, imagen);
            File.Move(rutaTemporal, rutaDestino, overwrite: true);
            return $"Imagen guardada en '{rutaDestino}' ({imagen.Length} bytes)";
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            IntentarEliminarTemporal(rutaTemporal);
            throw new OperacionRechazadaException($"No se pudo guardar la imagen: {excepcion.Message}");
        }
    }

    private string Enviar(int comando, string mensaje)
        => Solicitar(comando, TextoUtf8.Codificar(mensaje)).ContenidoComoTexto();

    private RespuestaDelServidor Solicitar(int comando, byte[] datos)
    {
        ObjectDisposedException.ThrowIf(_cerrada, this);
        _codec.Escribir(_stream, new Trama(comando, datos));
        Trama respuesta = _codec.Leer(_stream);
        // Sin header REQ/RES la única verificación es que la respuesta traiga el comando enviado.
        if (respuesta.Comando != comando)
        {
            throw new ProtocoloException(
                $"El cliente recibió el CMD inesperado {respuesta.Comando:D2}");
        }

        return Interpretar(respuesta);
    }

    private static RespuestaDelServidor Interpretar(Trama respuesta)
    {
        RespuestaDelServidor contenido = RespuestaDelServidor.Decodificar(respuesta);
        if (contenido.Estado == Estados.Error)
            throw new OperacionRechazadaException(contenido.ContenidoComoTexto());
        if (contenido.Estado == Estados.Interno)
            throw new ErrorInternoDelServidorException(contenido.ContenidoComoTexto());

        return contenido;
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
