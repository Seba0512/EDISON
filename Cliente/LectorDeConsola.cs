using System.Text;
using Common;

namespace Cliente;

// Todas las lecturas comparten el mismo esqueleto: pedir, convertir, validar y reintentar.
// Devolver null significa siempre lo mismo: se acabó la entrada.
internal static class LectorDeConsola
{
    // Al leer entrada redirigida en UTF-8, .NET mete un carácter invisible al inicio y hay que sacarlo.
    private const char Bom = '\uFEFF';
    private const string Afirmacion = "s";

    public static int? LeerOpcion(int cantidadDeOpciones)
    {
        Consola.Prompt("Ingrese opción: ");
        while (true)
        {
            string? entrada = LeerLinea();
            if (entrada is null)
                return null;
            if (int.TryParse(entrada, out int opcion) && opcion >= 1 && opcion <= cantidadDeOpciones)
                return opcion - 1;

            Consola.Error($"Ingrese un número entre 1 y {cantidadDeOpciones}.");
            Consola.Prompt("Ingrese opción válida: ");
        }
    }

    public static string? LeerTexto(string mensaje, Func<string, string?>? validar = null)
    {
        Consola.Info(mensaje);
        while (true)
        {
            string? entrada = LeerLinea();
            if (entrada is null)
                return null;

            string texto = entrada.Trim();
            if (texto.Length == 0)
            {
                Consola.Error("El texto no puede estar vacío.");
                continue;
            }

            if (EsAceptable(texto, validar))
                return texto;
        }
    }

    // Enter vacío omite el campo. Se devuelve el valor junto con el aviso de entrada agotada,
    // en vez de un parámetro de salida, porque son dos desenlaces distintos.
    public static (bool Cancelada, string? Valor) LeerTextoOpcional(
        string mensaje, Func<string, string?>? validar = null)
    {
        Consola.Info(mensaje);
        while (true)
        {
            string? entrada = LeerLinea();
            if (entrada is null)
                return (true, null);

            string texto = entrada.Trim();
            if (texto.Length == 0)
                return (false, null);

            if (EsAceptable(texto, validar))
                return (false, texto);
        }
    }

    public static decimal? LeerDecimal(string mensaje, Func<decimal, string?>? validar = null)
        => LeerConvertido(mensaje, FormatoDeCampos.ADecimal, validar);

    public static DateTime? LeerFecha(string mensaje, Func<DateTime, string?>? validar = null)
        => LeerConvertido(mensaje, FormatoDeCampos.AFecha, validar);

    public static int? LeerEntero(string mensaje, Func<int, string?>? validar = null)
        => LeerConvertido(mensaje, FormatoDeCampos.AEntero, validar);

    public static Guid? LeerGuid(string mensaje)
        => LeerConvertido<Guid>(mensaje, FormatoDeCampos.AGuid, null);

    public static (bool Cancelada, decimal? Valor) LeerDecimalOpcional(string mensaje)
        => LeerConvertidoOpcional(mensaje, FormatoDeCampos.ADecimal);

    public static (bool Cancelada, DateTime? Valor) LeerFechaOpcional(string mensaje)
        => LeerConvertidoOpcional(mensaje, FormatoDeCampos.AFecha);

    public static (bool Cancelada, int? Valor) LeerEnteroOpcional(string mensaje)
        => LeerConvertidoOpcional(mensaje, FormatoDeCampos.AEntero);

    public static bool Confirmar(string mensaje)
    {
        string? respuesta = LeerTexto(mensaje);
        return respuesta is not null && respuesta.Equals(Afirmacion, StringComparison.OrdinalIgnoreCase);
    }

    public static Credenciales? LeerCredenciales(
        Func<string, string?> validarNombreUsuario, Func<string, string?> validarClave)
    {
        string? nombreUsuario = LeerTexto("Nombre de usuario:", validarNombreUsuario);
        if (nombreUsuario is null)
            return null;

        string? clave = LeerClave("Clave:", validarClave);
        if (clave is null)
            return null;

        Console.WriteLine();
        return new Credenciales(nombreUsuario, clave);
    }

    // Enter vacío significa sin filtro. Solo el fin de la entrada cancela toda la búsqueda.
    public static FiltrosDeBusqueda? LeerFiltros()
    {
        (bool sinEntrada, string? texto) = LeerTextoOpcional(
            "Texto a buscar en el nombre (Enter para omitir):", Validaciones.SinSeparador);
        if (sinEntrada)
            return null;

        (bool canceloPrecioMinimo, decimal? precioMinimo) =
            LeerDecimalOpcional("Precio mínimo (Enter para omitir):");
        if (canceloPrecioMinimo)
            return null;

        (bool canceloPrecioMaximo, decimal? precioMaximo) =
            LeerDecimalOpcional("Precio máximo (Enter para omitir):");
        if (canceloPrecioMaximo)
            return null;

        (bool canceloFecha, DateTime? fechaEntregaMaxima) = LeerFechaOpcional(
            "Fecha de entrega máxima, formato dd-MM-yyyy (Enter para omitir):");
        if (canceloFecha)
            return null;

        (bool canceloMinimo, int? minimoDisponibles) =
            LeerEnteroOpcional("Mínimo de unidades disponibles (Enter para omitir):");
        if (canceloMinimo)
            return null;

        return new FiltrosDeBusqueda(
            texto, precioMinimo, precioMaximo, fechaEntregaMaxima, minimoDisponibles);
    }

    // Se pregunta por la imagen antes de confirmar la creación del modelo.
    public static string? PreguntarRutaDeImagen()
    {
        if (!Confirmar("¿Desea subir una imagen para el modelo? (s/n):"))
            return null;

        string? rutaArchivo = LeerTexto("Ruta local del archivo de imagen:");
        if (rutaArchivo is null)
            return null;

        rutaArchivo = SinComillas(rutaArchivo);
        if (!File.Exists(rutaArchivo))
        {
            Consola.Error($"No se encontró el archivo '{rutaArchivo}'.");
            return null;
        }

        return rutaArchivo;
    }

    // La ruta viene de PreguntarRutaDeImagen: null significa que no se ofreció o se rechazó la imagen.
    public static void SubirImagenSiHayRuta(
        ConexionConServidor conexion, string nombreModelo, string? rutaArchivo)
    {
        if (rutaArchivo is null)
            return;

        Consola.Exito(conexion.SubirImagen(nombreModelo, rutaArchivo));
    }

    public static void EsperarEnter()
    {
        Console.WriteLine();
        Consola.Sutil("Presione Enter para continuar...");
        Console.ReadLine();
    }

    // Windows "Copiar como ruta de acceso" envuelve la ruta entre comillas dobles.
    private static string SinComillas(string ruta)
        => ruta.Length >= 2 && ruta[0] == '"' && ruta[^1] == '"'
            ? ruta[1..^1]
            : ruta;

    private static T? LeerConvertido<T>(
        string mensaje, Func<string, T> convertir, Func<T, string?>? validar) where T : struct
    {
        while (true)
        {
            string? texto = LeerTexto(mensaje);
            if (texto is null)
                return null;

            if (Convertido(texto, convertir) is T valor && EsAceptable(valor, validar))
                return valor;
        }
    }

    private static (bool Cancelada, T? Valor) LeerConvertidoOpcional<T>(
        string mensaje, Func<string, T> convertir) where T : struct
    {
        while (true)
        {
            (bool cancelada, string? texto) = LeerTextoOpcional(mensaje);
            if (cancelada)
                return (true, null);
            if (texto is null)
                return (false, null);

            if (Convertido(texto, convertir) is T valor)
                return (false, valor);
        }
    }

    // Un formato inválido no cancela nada: se avisa y se vuelve a pedir el mismo campo.
    private static T? Convertido<T>(string texto, Func<string, T> convertir) where T : struct
    {
        try
        {
            return convertir(texto);
        }
        catch (ProtocoloException excepcion)
        {
            Consola.Error(excepcion.Message);
            return null;
        }
    }

    private static bool EsAceptable<T>(T valor, Func<T, string?>? validar)
    {
        string? error = validar?.Invoke(valor);
        if (error is null)
            return true;

        Consola.Error(error);
        return false;
    }

    private static string? LeerClave(string mensaje, Func<string, string?>? validar)
    {
        Consola.Info(mensaje);
        while (true)
        {
            string? clave = LeerClaveEnmascarada();
            if (clave is null)
                return null;

            if (string.IsNullOrWhiteSpace(clave))
            {
                Consola.Error("La clave no puede estar vacía.");
                continue;
            }

            if (EsAceptable(clave, validar))
                return clave;
        }
    }

    // Con entrada redirigida (por ejemplo, corrección automatizada por pipe) no hay
    // terminal que enmascarar: se cae al ReadLine normal, que es lo único que funciona ahí.
    private static string? LeerClaveEnmascarada()
    {
        if (Console.IsInputRedirected)
            return LeerLinea();

        StringBuilder clave = new();
        while (true)
        {
            ConsoleKeyInfo tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return clave.ToString();
            }

            if (tecla.Key == ConsoleKey.Backspace)
            {
                BorrarUnCaracter(clave);
                continue;
            }

            if (!char.IsControl(tecla.KeyChar))
            {
                clave.Append(tecla.KeyChar);
                Console.Write('*');
            }
        }
    }

    private static void BorrarUnCaracter(StringBuilder clave)
    {
        if (clave.Length == 0)
            return;

        clave.Length--;
        Console.Write("\b \b");
    }

    // ReadLine devuelve null cuando se acaba la entrada. Eso se toma como salida normal, no como error.
    private static string? LeerLinea()
        => Console.ReadLine()?.TrimStart(Bom);
}

// Replica en el Cliente las reglas de negocio que valida el Dominio del lado del Servidor.
// El objetivo es dar feedback inmediato campo a campo, sin esperar al rechazo del servidor al
// final del formulario. El Cliente no referencia el ensamblado Dominio (son procesos separados
// que se comunican por protocolo), así que estas constantes se duplican a propósito: el
// Servidor sigue siendo la autoridad final si las reglas cambian allá.
internal static class Validaciones
{
    private const int LargoMinimoNombreUsuario = 3;
    private const int LargoMaximoNombreUsuario = 20;
    private const int LargoMinimoClave = 4;
    private const int LargoMinimoNombreDeModelo = 2;
    private const int LargoMaximoNombreDeModelo = 60;
    private const int LargoMaximoDescripcion = 500;

    // El '#' es el separador de campos del protocolo: dentro de un campo rompería el mensaje.
    public static string? SinSeparador(string texto)
        => texto.Contains(Mensajes.Separador)
            ? $"El texto no puede contener el carácter '{Mensajes.Separador}'"
            : null;

    public static string? NombreUsuario(string texto)
    {
        if (SinSeparador(texto) is string error)
            return error;

        return texto.Length is < LargoMinimoNombreUsuario or > LargoMaximoNombreUsuario
            ? $"El nombre de usuario debe tener entre {LargoMinimoNombreUsuario} y {LargoMaximoNombreUsuario} caracteres"
            : null;
    }

    public static string? Clave(string texto)
    {
        if (SinSeparador(texto) is string error)
            return error;

        return texto.Length < LargoMinimoClave
            ? $"La clave debe tener al menos {LargoMinimoClave} caracteres"
            : null;
    }

    public static string? NombreDeModelo(string texto)
    {
        if (SinSeparador(texto) is string error)
            return error;

        return texto.Length is < LargoMinimoNombreDeModelo or > LargoMaximoNombreDeModelo
            ? $"El nombre del modelo debe tener entre {LargoMinimoNombreDeModelo} y {LargoMaximoNombreDeModelo} caracteres"
            : null;
    }

    public static string? Descripcion(string texto)
    {
        if (SinSeparador(texto) is string error)
            return error;

        return texto.Length > LargoMaximoDescripcion
            ? $"La descripción no puede superar los {LargoMaximoDescripcion} caracteres"
            : null;
    }

    // El servidor redondea el precio a entero antes de exigir que sea mayor a cero.
    public static string? Precio(decimal valor)
        => Math.Round(valor, 0, MidpointRounding.AwayFromZero) <= 0
            ? "El precio debe ser mayor a cero"
            : null;

    public static string? FechaFutura(DateTime valor)
        => valor.Date <= DateTime.Today
            ? "La fecha de entrega debe ser futura"
            : null;

    public static string? FechaFuturaSiCambio(DateTime nueva, DateTime actual)
        => nueva.Date != actual.Date ? FechaFutura(nueva) : null;

    public static string? Stock(int valor)
        => valor <= 0 ? "El stock debe ser mayor a cero" : null;

    // La modificación reconstruye la misma ficha que el alta, así que el stock nuevo también
    // debe ser mayor a cero, además de respetar reservas y disponibilidad actuales.
    public static string? StockDeModificacion(int nuevoStock, DetalleDeModelo detalleActual)
    {
        string? errorBase = Stock(nuevoStock);
        if (errorBase is not null)
            return errorBase;

        int stockTotalActual = detalleActual.UnidadesDisponibles + detalleActual.UnidadesReservadas;

        if (detalleActual.UnidadesDisponibles == 0 && nuevoStock <= stockTotalActual)
            return $"No se puede modificar el modelo {detalleActual.Nombre} porque no quedan unidades disponibles";

        if (nuevoStock != stockTotalActual && nuevoStock < detalleActual.UnidadesReservadas)
            return $"El stock no puede quedar por debajo de las {detalleActual.UnidadesReservadas} unidades reservadas";

        return null;
    }
}
