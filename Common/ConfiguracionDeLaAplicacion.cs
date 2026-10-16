using System.Configuration;
using System.Globalization;
using System.Net;

namespace Common;

// Punto unico de lectura del App.config. Cliente y Servidor validaban por separado, con el
// mismo codigo, las mismas dos cosas: una direccion IP y un puerto. En el App.config solo
// queda lo que depende del entorno; los limites del protocolo estan en Common (Limites).
public static class ConfiguracionDeLaAplicacion
{
    public static string Leer(string clave)
    {
        string? valor = Buscar(clave);
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ConfigurationErrorsException($"Falta la clave '{clave}' en App.config");
        }

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
        {
            return numero;
        }

        throw new ConfigurationErrorsException(
            $"La clave '{clave}' debe contener un número entero válido");
    }

    public static IPAddress LeerDireccion(string clave)
    {
        string valor = Leer(clave);
        if (IPAddress.TryParse(valor, out IPAddress? direccion))
        {
            return direccion;
        }

        throw new ConfigurationErrorsException(
            $"La clave '{clave}' debe contener una dirección IP válida");
    }

    public static int LeerPuerto(string clave)
    {
        int puerto = LeerEntero(clave);
        if (puerto is > IPEndPoint.MinPort and <= IPEndPoint.MaxPort)
        {
            return puerto;
        }

        throw new ConfigurationErrorsException(
            $"La clave '{clave}' debe estar entre 1 y {IPEndPoint.MaxPort}");
    }

    private static string? Buscar(string clave)
        => ConfigurationManager.AppSettings[clave];
}
