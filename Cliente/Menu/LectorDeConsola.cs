using Cliente.Vistas;
using Common.Excepciones;
using Common.Protocolo;

namespace Cliente.Menu;

internal static class LectorDeConsola
{
    // Al leer entrada redirigida en UTF-8, .NET mete un carácter invisible al inicio y hay que sacarlo.
    private const char Bom = '\uFEFF';

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

    public static string? LeerTexto(string mensaje)
    {
        Consola.Info(mensaje);
        while (true)
        {
            string? entrada = LeerLinea();
            if (entrada is null)
                return null;

            string texto = entrada.Trim();
            if (texto.Length > 0)
                return texto;

            Consola.Error("El texto no puede estar vacío.");
        }
    }

    public static void EsperarEnter()
    {
        Console.WriteLine();
        Consola.Sutil("Presione Enter para continuar...");
        Console.ReadLine();
    }

    public static bool LeerTextoOpcional(string mensaje, out string? valor)
    {
        Consola.Info(mensaje);
        string? entrada = LeerLinea();
        if (entrada is null)
        {
            valor = null;
            return false;
        }

        string texto = entrada.Trim();
        valor = texto.Length == 0 ? null : texto;
        return true;
    }

    public static bool Confirmar(string mensaje)
    {
        string? respuesta = LeerTexto(mensaje);
        return respuesta is not null && respuesta.Equals("s", StringComparison.OrdinalIgnoreCase);
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
                Consola.Error(excepcion.Message);
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
                Consola.Error(excepcion.Message);
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
                Consola.Error(excepcion.Message);
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
                Consola.Error(excepcion.Message);
            }
        }
    }

    // ReadLine devuelve null cuando se acaba la entrada. Eso se toma como salida normal, no como error.
    private static string? LeerLinea()
        => Console.ReadLine()?.TrimStart(Bom);

    public static Credenciales? LeerCredenciales()
    {
        string? nombreUsuario = LeerTexto("Nombre de usuario:");
        if (nombreUsuario is null)
            return null;

        string? clave = LeerClave("Clave:");
        if (clave is null)
            return null;

        Console.WriteLine();
        return new Credenciales(nombreUsuario, clave);
    }

    private static string? LeerClave(string mensaje)
    {
        Consola.Info(mensaje);
        while (true)
        {
            string? clave = LeerClaveEnmascarada();
            if (clave is null)
                return null;
            if (!string.IsNullOrWhiteSpace(clave))
                return clave;

            Consola.Error("La clave no puede estar vacía.");
        }
    }

    // Con entrada redirigida (por ejemplo, corrección automatizada por pipe) no hay
    // terminal que enmascarar: se cae al ReadLine normal, que es lo único que funciona ahí.
    private static string? LeerClaveEnmascarada()
    {
        if (Console.IsInputRedirected)
            return LeerLinea();

        System.Text.StringBuilder clave = new();
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
                if (clave.Length > 0)
                {
                    clave.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(tecla.KeyChar))
            {
                clave.Append(tecla.KeyChar);
                Console.Write('*');
            }
        }
    }
}
