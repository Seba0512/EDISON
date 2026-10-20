using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Common;

namespace Cliente;

internal sealed class ConexionConServidor : IAsyncDisposable
{
    private const string SufijoTemporal = ".tmp";
    // Las partes llegan de a 32 KiB: sin buffer, una falla del disco aparece al escribir esa parte,
    // donde se la atrapa con la conexión sincronizada, y no recién al cerrar el archivo.
    private const int SinBuffer = 0;

    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private readonly CodecDeTramas _codec;
    private readonly TransferenciaPorPartes _archivos;
    private readonly string _downloadsDirectory;
    private bool _cerrada;

    private ConexionConServidor(Socket socket, ConfiguracionCliente configuracion)
    {
        _socket = socket;
        // ownsSocket en false porque Dispose ya se encarga de cerrar el stream y el socket en orden.
        _stream = new NetworkStream(socket, ownsSocket: false);
        _codec = new CodecDeTramas();
        _archivos = new TransferenciaPorPartes(_stream);
        _downloadsDirectory = Path.GetFullPath(configuracion.DownloadsDirectory);
    }

    public static async Task<ConexionConServidor> ConectarAsync(ConfiguracionCliente configuracion)
    {
        // Sin Bind local: el sistema operativo asigna el puerto efímero, así pueden abrirse varias
        // instancias del cliente y conectarse a un servidor en otra máquina sin tocar código.
        // Sin familia de direcciones el socket es dual (IPv4 e IPv6).
        Socket socket = new(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            // El host puede ser una IP o un nombre (en Docker, el del contenedor). ConnectAsync lo
            // resuelve y prueba sus direcciones en orden hasta que una acepta: en Windows "localhost"
            // da primero ::1, y el servidor por defecto escucha solo en IPv4.
            await socket.ConnectAsync(configuracion.ServerHost, configuracion.ServerPort);
            return new ConexionConServidor(socket, configuracion);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public Task<string> RegistrarAsync(Credenciales credenciales)
        => EnviarAsync(Comandos.Registrar, Mensajes.DeCredenciales(credenciales));

    public Task<string> IniciarSesionAsync(Credenciales credenciales)
        => EnviarAsync(Comandos.IniciarSesion, Mensajes.DeCredenciales(credenciales));

    public Task<string> CerrarSesionAsync()
        => EnviarAsync(Comandos.CerrarSesion, string.Empty);

    public Task<string> AltaDeModeloAsync(DatosDeAltaDeModelo datos)
        => EnviarAsync(Comandos.AltaDeModelo, Mensajes.DeDatosDeModelo(datos));

    public Task<string> ModificarModeloAsync(DatosDeAltaDeModelo datos)
        => EnviarAsync(Comandos.ModificarModelo, Mensajes.DeDatosDeModelo(datos));

    public Task<string> EliminarModeloAsync(string nombreModelo)
        => EnviarAsync(Comandos.BajaDeModelo, nombreModelo);

    public async Task<IReadOnlyList<ModeloListado>> BuscarAsync(FiltrosDeBusqueda filtros)
        => Mensajes.AModelosListados(
            await EnviarAsync(Comandos.BuscarModelos, Mensajes.DeFiltros(filtros)));

    public async Task<DetalleDeModelo> ConsultarAsync(string nombreModelo)
        => Mensajes.ADetalleDeModelo(await EnviarAsync(Comandos.ConsultarModelo, nombreModelo));

    public Task<string> SolicitarReservaAsync(string nombreModelo)
        => EnviarAsync(Comandos.SolicitarReserva, nombreModelo);

    public Task<string> CancelarReservaAsync(Guid idReserva)
        => EnviarAsync(Comandos.CancelarReserva, FormatoDeCampos.De(idReserva));

    public async Task<IReadOnlyList<ReservaListada>> MisReservasAsync()
        => Mensajes.AReservasListadas(await EnviarAsync(Comandos.ConsultarMisReservas, string.Empty));

    public async Task<IReadOnlyList<ActividadListada>> HistorialAsync()
        => Mensajes.AActividades(await EnviarAsync(Comandos.ConsultarHistorial, string.Empty));

    // La subida va en dos pasos: se anuncia la imagen y solo con el OK del servidor se mandan sus
    // partes, así un rechazo de entrada no hace viajar ningún byte del archivo.
    public async Task<string> SubirImagenAsync(string nombreModelo, string rutaArchivo)
    {
        await using FileStream imagen = AbrirArchivoLocal(rutaArchivo);
        ExigirTamañoPermitido(imagen.Length);
        SubidaDeImagen subida = new(nombreModelo, Path.GetFileName(rutaArchivo), imagen.Length);
        await EnviarAsync(Comandos.SubirImagen, Mensajes.DeSubidaDeImagen(subida));
        await _archivos.EnviarAsync(imagen, subida.Largo);
        return (await RecibirRespuestaAsync(Comandos.SubirImagen)).ContenidoComoTexto();
    }

    // El OK anuncia nombre y largo, y las partes llegan a continuación: cada una se escribe
    // directo al archivo temporal, sin juntar la imagen entera en memoria.
    public async Task<string> DescargarImagenAsync(string nombreModelo)
    {
        string rutaTemporal =
            Path.Combine(_downloadsDirectory, $"{Guid.NewGuid():N}{SufijoTemporal}");
        try
        {
            ArchivoAnunciado imagen = await RecibirImagenAsync(nombreModelo, rutaTemporal);
            return MoverADescargas(rutaTemporal, imagen);
        }
        finally
        {
            IntentarEliminarTemporal(rutaTemporal);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cerrada)
            return;

        _cerrada = true;
        IntentarShutdown();
        await _stream.DisposeAsync();
        _socket.Dispose();
    }

    // Un problema local con el archivo todavía no tocó la red, así que la sesión puede continuar.
    private static FileStream AbrirArchivoLocal(string rutaArchivo)
    {
        try
        {
            return File.OpenRead(rutaArchivo);
        }
        // El Message de .NET viene en inglés y es técnico: al usuario se le dice qué revisar.
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            throw new OperacionRechazadaException(
                $"No se pudo leer el archivo '{rutaArchivo}'. Verifique que tenga permisos de lectura " +
                "y que no esté abierto en otro programa.");
        }
    }

    private static void ExigirTamañoPermitido(long largo)
    {
        if (largo == 0)
        {
            throw new OperacionRechazadaException("El archivo está vacío: no es una imagen válida");
        }

        if (largo > Limites.LargoMaximoDeArchivo)
        {
            throw new OperacionRechazadaException("El archivo supera el tamaño máximo permitido " +
                $"({Limites.LargoMaximoDeArchivo} bytes)");
        }
    }

    // El archivo local se crea antes de pedir la imagen: si falla, todavía no viajó nada por la
    // red. Si falla a mitad, Recibir igual consume las partes y la conexión queda sana.
    private async Task<ArchivoAnunciado> RecibirImagenAsync(string nombreModelo, string rutaTemporal)
    {
        await using FileStream destino = CrearArchivoLocal(rutaTemporal);
        string anuncio = await EnviarAsync(Comandos.DescargarImagen, nombreModelo);
        ArchivoAnunciado anunciado = Mensajes.AArchivoAnunciado(anuncio);
        // El nombre se valida con el anuncio, antes de recibir: si no sirve, no se baja ningún byte.
        ArchivoAnunciado imagen = anunciado with { Nombre = NombreDeDescargaSeguro.Crear(anunciado.Nombre) };
        try
        {
            await _archivos.RecibirAsync(destino, imagen.Largo);
        }
        catch (ArchivoLocalException)
        {
            throw NoSePudoGuardar();
        }

        return imagen;
    }

    private FileStream CrearArchivoLocal(string ruta)
    {
        try
        {
            Directory.CreateDirectory(_downloadsDirectory);
            return new FileStream(
                ruta, FileMode.Create, FileAccess.Write, FileShare.None, SinBuffer, useAsync: true);
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            throw NoSePudoGuardar();
        }
    }

    // Se recibe a un .tmp y recién completo se mueve, para no dejar una imagen a medias.
    private string MoverADescargas(string rutaTemporal, ArchivoAnunciado imagen)
    {
        string rutaDestino = Path.Combine(_downloadsDirectory, imagen.Nombre);
        try
        {
            File.Move(rutaTemporal, rutaDestino, overwrite: true);
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            throw NoSePudoGuardar();
        }

        return $"Imagen guardada en '{rutaDestino}' ({imagen.Largo} bytes)";
    }

    private OperacionRechazadaException NoSePudoGuardar()
    {
        return new OperacionRechazadaException(
            $"No se pudo guardar la imagen en '{_downloadsDirectory}'. Verifique que la carpeta " +
            "tenga permisos de escritura y que el archivo no esté abierto en otro programa.");
    }

    private async Task<string> EnviarAsync(int comando, string mensaje)
        => (await SolicitarAsync(comando, TextoUtf8.Codificar(mensaje))).ContenidoComoTexto();

    private async Task<RespuestaDelServidor> SolicitarAsync(int comando, byte[] datos)
    {
        ObjectDisposedException.ThrowIf(_cerrada, this);
        await _codec.EscribirAsync(_stream, new Trama(comando, datos));
        return await RecibirRespuestaAsync(comando);
    }

    // La subida espera dos respuestas al mismo comando: la que habilita las partes y la final.
    private async Task<RespuestaDelServidor> RecibirRespuestaAsync(int comando)
    {
        Trama respuesta = await _codec.LeerAsync(_stream);
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

    private static void IntentarEliminarTemporal(string rutaTemporal)
    {
        if (!File.Exists(rutaTemporal))
            return;

        try
        {
            File.Delete(rutaTemporal);
        }
        catch (Exception)
        {
            Console.Error.WriteLine($"No se pudo borrar el archivo temporal '{rutaTemporal}'.");
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
