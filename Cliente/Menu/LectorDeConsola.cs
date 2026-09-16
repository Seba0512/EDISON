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
