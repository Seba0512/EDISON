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

    // El nombre de usuario se usa como carpeta, así que no puede salirse del directorio ni ser
    // un nombre que el sistema de archivos trate distinto.
    public static string ValidarNombreDeUsuario(string nombreUsuario)
    {
        string nombre = (nombreUsuario ?? string.Empty).Trim();
        if (nombre.Length == 0
            || Path.IsPathRooted(nombre)
            || nombre.Contains('/')
            || nombre.Contains('\\')
            || nombre.Contains("..")
            || nombre.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || nombre.EndsWith('.')
            || NombresReservados.Contains(nombre.Split('.')[0]))
        {
            throw new ProtocoloException(
                "El nombre de usuario no se puede usar como carpeta de comprobantes");
        }

        return nombre;
    }

    private string CarpetaDe(string nombreUsuario)
    {
        string carpeta = Path.GetFullPath(Path.Combine(_directorio, ValidarNombreDeUsuario(nombreUsuario)));
        ExigirDentroDelDirectorio(carpeta);
        return carpeta;
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
