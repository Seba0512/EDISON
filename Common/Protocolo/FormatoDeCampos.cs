using System.Globalization;
using Common.Excepciones;

namespace Common.Protocolo;

/// <summary>
/// Conversión texto ↔ <c>int</c>/<c>decimal</c>/<c>DateTime</c>/<c>Guid</c> para los campos de
/// CR2-CR10, toda con <see cref="CultureInfo.InvariantCulture"/>: el cable no puede depender de la
/// configuración regional de la máquina que corre el cliente o el servidor.
///
/// Todo fallo de conversión se traduce a <see cref="ProtocoloException"/>: un campo mal formado es un
/// error de protocolo recuperable (PR) y no una <see cref="FormatException"/> sin atrapar que termine
/// cerrando la conexión con IN.
/// </summary>
public static class FormatoDeCampos
{
    private const string FormatoFecha = "dd-MM-yyyy";
    private const string FormatoFechaHora = "yyyyMMddHHmmss";

    public static string De(int valor)
        => valor.ToString(CultureInfo.InvariantCulture);

    public static string De(decimal valor)
        => valor.ToString(CultureInfo.InvariantCulture);

    public static string DeFecha(DateTime fecha)
        => fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture);

    /// <summary>El valor se asume UTC, criterio único del protocolo para no arrastrar zonas horarias.</summary>
    public static string DeFechaHora(DateTime fechaHora)
        => fechaHora.ToUniversalTime().ToString(FormatoFechaHora, CultureInfo.InvariantCulture);

    public static string De(Guid valor)
        => valor.ToString("D");

    public static int AEntero(string texto)
    {
        if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor))
            return valor;

        throw new ProtocoloException($"'{texto}' no es un entero válido");
    }

    public static decimal ADecimal(string texto)
    {
        if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valor))
            return valor;

        throw new ProtocoloException($"'{texto}' no es un decimal válido");
    }

    public static DateTime AFecha(string texto)
    {
        if (DateTime.TryParseExact(
                texto, FormatoFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime valor))
            return valor;

        throw new ProtocoloException($"'{texto}' no es una fecha válida (formato {FormatoFecha})");
    }

    public static DateTime AFechaHora(string texto)
    {
        if (DateTime.TryParseExact(
                texto,
                FormatoFechaHora,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime valor))
            return valor;

        throw new ProtocoloException(
            $"'{texto}' no es una fecha/hora válida (formato {FormatoFechaHora})");
    }

    public static Guid AGuid(string texto)
    {
        if (Guid.TryParseExact(texto, "D", out Guid valor))
            return valor;

        throw new ProtocoloException($"'{texto}' no es un Guid válido");
    }
}
