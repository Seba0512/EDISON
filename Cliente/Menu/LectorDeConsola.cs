using Cliente.Vistas;
using Common.Excepciones;
using Common.Protocolo;

namespace Cliente.Menu;

internal static class LectorDeConsola
{
    // Al leer entrada redirigida en UTF-8, .NET mete un carácter invisible al inicio y hay que sacarlo.
    private const char Bom = '﻿';

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

    // El validador es opcional: quien no lo pasa mantiene el comportamiento de siempre
    // (solo se exige que el texto no esté vacío). Quien lo pasa reintenta el mismo campo
    // ante una regla de negocio, igual que ya se hace con los errores de formato.
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

            string? error = validar?.Invoke(texto);
            if (error is not null)
            {
                Consola.Error(error);
                continue;
            }

            return texto;
        }
    }

    public static void EsperarEnter()
    {
        Console.WriteLine();
        Consola.Sutil("Presione Enter para continuar...");
        Console.ReadLine();
    }

    public static bool LeerTextoOpcional(string mensaje, out string? valor, Func<string, string?>? validar = null)
    {
        Consola.Info(mensaje);
        while (true)
        {
            string? entrada = LeerLinea();
            if (entrada is null)
            {
                valor = null;
                return false;
            }

            string texto = entrada.Trim();
            if (texto.Length == 0)
            {
                valor = null;
                return true;
            }

            string? error = validar?.Invoke(texto);
            if (error is not null)
            {
                Consola.Error(error);
                continue;
            }

            valor = texto;
            return true;
        }
    }

    public static bool Confirmar(string mensaje)
    {
        string? respuesta = LeerTexto(mensaje);
        return respuesta is not null && respuesta.Equals("s", StringComparison.OrdinalIgnoreCase);
    }

    public static decimal? LeerDecimal(string mensaje, Func<decimal, string?>? validar = null)
    {
        while (true)
        {
            string? texto = LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                decimal valor = FormatoDeCampos.ADecimal(texto);
                string? error = validar?.Invoke(valor);
                if (error is not null)
                {
                    Consola.Error(error);
                    continue;
                }

                return valor;
            }
            catch (ProtocoloException excepcion)
            {
                Consola.Error(excepcion.Message);
            }
        }
    }

    public static DateTime? LeerFecha(string mensaje, Func<DateTime, string?>? validar = null)
    {
        while (true)
        {
            string? texto = LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                DateTime valor = FormatoDeCampos.AFecha(texto);
                string? error = validar?.Invoke(valor);
                if (error is not null)
                {
                    Consola.Error(error);
                    continue;
                }

                return valor;
            }
            catch (ProtocoloException excepcion)
            {
                Consola.Error(excepcion.Message);
            }
        }
    }

    public static int? LeerEntero(string mensaje, Func<int, string?>? validar = null)
    {
        while (true)
        {
            string? texto = LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                int valor = FormatoDeCampos.AEntero(texto);
                string? error = validar?.Invoke(valor);
                if (error is not null)
                {
                    Consola.Error(error);
                    continue;
                }

                return valor;
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

    public static Credenciales? LeerCredenciales(
        Func<string, string?>? validarNombreUsuario = null, Func<string, string?>? validarClave = null)
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

    private static string? LeerClave(string mensaje, Func<string, string?>? validar = null)
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

            string? error = validar?.Invoke(clave);
            if (error is not null)
            {
                Consola.Error(error);
                continue;
            }

            return clave;
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
