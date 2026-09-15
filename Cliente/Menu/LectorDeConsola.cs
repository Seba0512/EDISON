using Common.Protocolo;

namespace Cliente.Menu;

/// <summary>
/// Lectura de consola validada: texto no vacío y opción numérica dentro de rango. Si
/// <see cref="Console.ReadLine"/> devuelve null (EOF), cada método devuelve null y quien lo llama lo
/// trata como un cierre normal, no como un error de red.
/// </summary>
internal static class LectorDeConsola
{
    /// <summary>
    /// Con <see cref="Console.InputEncoding"/> en UTF-8, .NET antepone un BOM fantasma a la primera
    /// línea leída de una entrada redirigida (un archivo, una tubería), sin que ese byte exista
    /// realmente en la entrada. Se descarta acá para no romper la primera opción o el primer campo.
    /// </summary>
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
