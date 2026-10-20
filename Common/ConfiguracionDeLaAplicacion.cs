using System.Globalization;
using System.Net;

namespace Common;

// Punto unico de lectura de la configuracion. Todo lo que depende del entorno llega por
// variables de entorno, que es lo que un contenedor sabe recibir (-e, environment:). Los
// limites del protocolo no se configuran: estan en Common (Limites).
public static class ConfiguracionDeLaAplicacion
{
    public static string Leer(string clave)
    {
        string? valor = Buscar(clave);
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ConfiguracionInvalidaException(
                $"Falta la variable de entorno '{clave}'");
        }

        return valor;
    }

    public static string Leer(string clave, string defecto)
    {
        string? valor = Buscar(clave);
        return string.IsNullOrWhiteSpace(valor) ? defecto : valor;
    }

    public static int LeerEntero(string clave, int defecto)
    {
        string? valor = Buscar(clave);
        return string.IsNullOrWhiteSpace(valor) ? defecto : ConvertirAEntero(clave, valor);
    }

    public static int LeerEntero(string clave)
    {
        return ConvertirAEntero(clave, Leer(clave));
    }

    public static IPAddress LeerDireccion(string clave, string defecto)
    {
        string valor = Leer(clave, defecto);
        if (IPAddress.TryParse(valor, out IPAddress? direccion))
        {
            return direccion;
        }

        throw new ConfiguracionInvalidaException(
            $"La variable '{clave}' debe contener una dirección IP válida");
    }

    // Devuelve el texto tal cual: puede ser una IP o un nombre (por ejemplo el del contenedor
    // del servidor). Se resuelve recién al conectar, con Dns.GetHostAddressesAsync.
    public static string LeerHost(string clave, string defecto)
    {
        return Leer(clave, defecto).Trim();
    }

    public static int LeerPuerto(string clave, int defecto)
    {
        int puerto = LeerEntero(clave, defecto);
        if (puerto is > IPEndPoint.MinPort and <= IPEndPoint.MaxPort)
        {
            return puerto;
        }

        throw new ConfiguracionInvalidaException(
            $"La variable '{clave}' debe estar entre 1 y {IPEndPoint.MaxPort}");
    }

    private static int ConvertirAEntero(string clave, string valor)
    {
        if (int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numero))
        {
            return numero;
        }

        throw new ConfiguracionInvalidaException(
            $"La variable '{clave}' debe contener un número entero válido");
    }

    private static string? Buscar(string clave)
        => Environment.GetEnvironmentVariable(clave);
}
