using Common.Excepciones;
using Common.Protocolo;

namespace Cliente.Menu;

internal static class LectorDeConsola
{
    private const char Bom = '\uFEFF';

    public static int? LeerOpcion(int cantidadDeOpciones)
    {
        while (true)
        {
            string? entrada = LeerLinea();
            if (entrada is null)
                return null;
            if (int.TryParse(entrada, out int opcion) && opcion >= 1 && opcion <= cantidadDeOpciones)
                return opcion - 1;

            Console.WriteLine($"Ingrese un número entre 1 y {cantidadDeOpciones}.");
        }
    }

    public static string? LeerTexto(string mensaje)
    {
        Console.WriteLine(mensaje);
        while (true)
        {
            string? entrada = LeerLinea();
            if (entrada is null)
                return null;

            string texto = entrada.Trim();
            if (texto.Length > 0)
                return texto;

            Console.WriteLine("El texto no puede estar vacío.");
        }
    }

    public static decimal? LeerDecimal(string mensaje)
    {
        while (true)
        {
            string? texto = LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                return FormatoDeCampos.ADecimal(texto);
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }

    public static DateTime? LeerFecha(string mensaje)
    {
        while (true)
        {
            string? texto = LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                return FormatoDeCampos.AFecha(texto);
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }

    public static int? LeerEntero(string mensaje)
    {
        while (true)
        {
            string? texto = LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                return FormatoDeCampos.AEntero(texto);
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }

    public static Guid? LeerGuid(string mensaje)
    {
        while (true)
        {
            string? texto = LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                return FormatoDeCampos.AGuid(texto);
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }

    private static string? LeerLinea()
        => Console.ReadLine()?.TrimStart(Bom);

    public static Credenciales? LeerCredenciales()
    {
        string? nombreUsuario = LeerTexto("Nombre de usuario:");
        if (nombreUsuario is null)
            return null;

        string? clave = LeerTexto("Clave:");
        if (clave is null)
            return null;

        return new Credenciales(nombreUsuario, clave);
    }
}
