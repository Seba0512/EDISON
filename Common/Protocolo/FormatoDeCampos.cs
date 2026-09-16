using System.Globalization;
using Common.Excepciones;

namespace Common.Protocolo;

// Siempre InvariantCulture para que los datos no cambien según el idioma configurado en la máquina.
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
