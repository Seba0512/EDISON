using System.Configuration;
using System.Globalization;

namespace Common;

public static class ConfigManager
{
    private static string? Buscar(string clave)
        => ConfigurationManager.AppSettings[clave];

    public static string Leer(string clave)
    {
        string? valor = Buscar(clave);
        if (string.IsNullOrWhiteSpace(valor))
            throw new ConfigurationErrorsException(
                $"Falta la clave '{clave}' en App.config");

        return valor;
    }

    public static string Leer(string clave, string defecto)
    {
        string? valor = Buscar(clave);
        return string.IsNullOrWhiteSpace(valor) ? defecto : valor;
    }

    public static int LeerEntero(string clave)
    {
        string valor = Leer(clave);
        if (int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numero))
            return numero;

        throw new ConfigurationErrorsException(
            $"La clave '{clave}' debe contener un número entero válido");
    }
}
