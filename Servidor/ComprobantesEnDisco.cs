using System.Globalization;
using System.Text;
using Common;
using Servidor.Dominio.Entidades;
using Servidor.Dominio.Servicios;

namespace Servidor;

// Escribe DIRECTORIO_COMPROBANTES/<usuario>/<idReserva>.txt. El texto se arma a mano: el
// comprobante no pasa por ningún serializador.
internal sealed class ComprobantesEnDisco : IGeneradorDeComprobantes
{
    private const string FormatoDeFecha = "dd-MM-yyyy HH:mm:ss";

    private static readonly UTF8Encoding Codificacion = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly StringComparison ComparacionDeRutas =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    // Los que Windows no admite en un nombre de carpeta, más el propio '%' de los escapes.
    private static readonly HashSet<char> CaracteresQueSeEscapan =
        new("<>:\"/\\|?*%".Concat(Enumerable.Range(0, 32).Select(valor => (char)valor)));

    private static readonly HashSet<string> NombresReservados = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private readonly string _directorio;

    public ComprobantesEnDisco(string directorio)
    {
        _directorio = Path.GetFullPath(directorio);
        Directory.CreateDirectory(_directorio);
    }

    public async Task GenerarAsync(LecturaDeReserva reserva, decimal precio)
    {
        string carpeta = CarpetaDe(reserva.NombreSolicitante);
        Directory.CreateDirectory(carpeta);
        string ruta = Path.Combine(carpeta, $"{reserva.Id}.txt");
        ExigirDentroDelDirectorio(Path.GetFullPath(ruta));

        // Se escribe a un .tmp y luego se mueve, para no dejar un comprobante a medias si falla.
        string rutaTemporal = ruta + ".tmp";
        try
        {
            await File.WriteAllTextAsync(rutaTemporal, ArmarTexto(reserva, precio), Codificacion);
            File.Move(rutaTemporal, ruta, overwrite: true);
        }
        finally
        {
            if (File.Exists(rutaTemporal))
            {
                File.Delete(rutaTemporal);
            }
        }
    }

    private string CarpetaDe(string nombreUsuario)
    {
        string carpeta = Path.GetFullPath(Path.Combine(_directorio, CarpetaPara(nombreUsuario)));
        ExigirDentroDelDirectorio(carpeta);
        return carpeta;
    }

    // Toda identidad aceptada tiene su carpeta. Lo normal es usar el nombre tal cual, porque el
    // registro solo acepta nombres que sirven como carpeta. Uno que no sirve (por ejemplo, de un
    // usuario registrado antes de esa regla) se escribe con escapes %XXXX. Como el '%' también se
    // escapa, un nombre usado tal cual nunca coincide con uno escapado.
    private static string CarpetaPara(string nombreUsuario)
    {
        return EsCarpetaValida(nombreUsuario) ? nombreUsuario : ConEscapes(nombreUsuario);
    }

    // Criterio de Windows, el más estricto, en cualquier sistema: así la carpeta de un usuario es la
    // misma corra donde corra el servidor.
    private static bool EsCarpetaValida(string nombre)
    {
        return nombre.Length > 0
               && !nombre.Any(CaracteresQueSeEscapan.Contains)
               && nombre[0] is not ('.' or ' ')
               && nombre[^1] is not ('.' or ' ')
               && !EsReservado(nombre);
    }

    private static string ConEscapes(string nombre)
    {
        bool reservado = EsReservado(nombre);
        StringBuilder carpeta = new(nombre.Length);
        for (int indice = 0; indice < nombre.Length; indice++)
        {
            char caracter = nombre[indice];
            bool enUnExtremo = indice == 0 || indice == nombre.Length - 1;
            bool escapar = CaracteresQueSeEscapan.Contains(caracter)
                           || (enUnExtremo && caracter is '.' or ' ')
                           || (indice == 0 && reservado);
            carpeta.Append(escapar ? $"%{(int)caracter:X4}" : caracter);
        }

        return carpeta.ToString();
    }

    private static bool EsReservado(string nombre)
    {
        return NombresReservados.Contains(nombre.Split('.')[0].TrimEnd());
    }

    // Segunda barrera: aunque la validación del nombre falle, la ruta final tiene que quedar adentro.
    private void ExigirDentroDelDirectorio(string rutaCompleta)
    {
        string raizConSeparador = Path.EndsInDirectorySeparator(_directorio)
            ? _directorio
            : _directorio + Path.DirectorySeparatorChar;
        if (!rutaCompleta.StartsWith(raizConSeparador, ComparacionDeRutas))
        {
            throw new ProtocoloException("La ruta del comprobante queda fuera del directorio permitido");
        }
    }

    private static string ArmarTexto(LecturaDeReserva reserva, decimal precio)
    {
        StringBuilder texto = new();
        texto.AppendLine("COMPROBANTE DE RESERVA");
        texto.AppendLine($"Id de la reserva: {reserva.Id}");
        texto.AppendLine($"Usuario: {reserva.NombreSolicitante}");
        texto.AppendLine($"Modelo: {reserva.NombreModelo}");
        texto.AppendLine($"Precio: {precio.ToString("0.00", CultureInfo.InvariantCulture)}");
        texto.AppendLine($"Fecha de solicitud: {EnHoraLocal(reserva.FechaCreacion)}");
        texto.AppendLine($"Fecha de confirmación: {EnHoraLocal(reserva.UltimaActualizacion)}");
        return texto.ToString();
    }

    // Las fechas se guardan en UTC y se convierten a hora local solo para mostrarlas.
    private static string EnHoraLocal(DateTime fechaUtc)
    {
        DateTime local = fechaUtc.ToLocalTime();
        TimeSpan desfase = TimeZoneInfo.Local.GetUtcOffset(fechaUtc);
        string signo = desfase < TimeSpan.Zero ? "-" : "+";
        return $"{local.ToString(FormatoDeFecha, CultureInfo.InvariantCulture)} " +
               $"(UTC{signo}{desfase.Duration():hh\\:mm}, {TimeZoneInfo.Local.Id})";
    }
}
